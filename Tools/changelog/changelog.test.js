// Arcane-Start
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const yaml = require("js-yaml");
const { main, getChanges, github, parsePullRequest } = require("./changelog");
const { getMedia } = require("./media");

test("plain, bulleted and uppercase changes retain their types and categories", () => {
    assert.deepEqual(getChanges("add: New\n- FIX: Fixed\nTRAUMA:\n- tweak: Upstream"), {
        ARCANE: [{ type: "Add", message: "New" }, { type: "Fix", message: "Fixed" }],
        TRAUMA: [{ type: "Tweak", message: "Upstream" }],
    });
});

test("media comes only from visible Media sections, including nested headings", () => {
    const body = `## Медиа
![Image](https://example.org/image.png)
<!-- https://example.org/hidden.png -->
\`\`\`
https://example.org/example.png
\`\`\`
### Видео
https://example.org/video.mp4
https://example.org/image.png
## Описание
https://example.org/not-media.png`;
    assert.deepEqual(getMedia(body), ["https://example.org/image.png", "https://example.org/video.mp4"]);
    assert.deepEqual(getMedia(null), []);
});

test("merged PR becomes one Arcane entry with media; replay and null body are harmless", async () => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), "arcane-changelog-"));
    const cwd = process.cwd();
    const fetch = global.fetch;
    const env = { ...process.env };
    let body = `## Медиа\n![Screenshot](https://example.org/a.png)\n## Описание\n\`\`\`\n:cl: Example\n- add: Hidden\n\`\`\`\n:cl: Автор\n- fix: Исправлена ошибка.`;
    try {
        fs.mkdirSync(path.join(root, "Tools/changelog"), { recursive: true });
        fs.mkdirSync(path.join(root, "Resources/Changelog"), { recursive: true });
        const journal = path.join(root, "Resources/Changelog/ArcaneChangelog.yml");
        fs.writeFileSync(journal, "Name: Arcane\nOrder: -2\nEntries: []\n");
        process.chdir(path.join(root, "Tools/changelog"));
        process.env.PR_NUMBER = "42";
        process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
        global.fetch = async () => ({ ok: true, json: async () => ({
            body, number: 42, merged_at: "2026-11-04T00:00:00Z", user: { login: "fallback" },
            labels: [{ name: "Intent: Experimental" }],
        }) });
        const options = { directory: path.join(root, "Resources/Changelog") };
        await main(options);
        await main(options);
        body = null;
        await main(options);
        const result = yaml.load(fs.readFileSync(journal, "utf8"));
        assert.equal(result.Entries.length, 1);
        assert.equal(result.Entries[0].author, "Автор");
        assert.deepEqual(result.Entries[0].changes, [{ type: "Fix", message: "Исправлена ошибка." }]);
        assert.deepEqual(result.Entries[0].media, ["https://example.org/a.png"]);
        assert.deepEqual(result.Entries[0].labels, ["Intent: Experimental"]);
        assert.equal(result.Entries[0].prNumber, 42);
    } finally {
        process.chdir(cwd);
        global.fetch = fetch;
        process.env = env;
        fs.rmSync(root, { recursive: true });
    }
});
function fixture() {
    const directory = fs.mkdtempSync(path.join(os.tmpdir(), "arcane-batch-"));
    fs.writeFileSync(path.join(directory, "ArcaneChangelog.yml"), "Name: Arcane\nOrder: -3\nEntries: []\n");
    return directory;
}

function pull(number, body = ":cl:\n- add: Изменение " + number) {
    return { number, body, merged_at: "2026-11-04T00:00:00Z", base: { ref: "master" },
        user: { login: "Author" }, merge_commit_sha: "merge" + number };
}

test("reconciliation recovers intermediate merges in one write and is a no-op on replay", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    process.env.CHANGELOG_BRANCH = "master";
    const prs = Array.from({ length: 250 }, (_, i) => pull(i + 1));
    let requests = 0;
    const request = async uri => {
        requests++;
        const page = Number(new URL(uri).searchParams.get("page"));
        return { ok: true, json: async () => prs.slice((page - 1) * 100, page * 100) };
    };
    try {
        const first = await main({ directory, reconcile: true, request });
        assert.deepEqual(first, { checked: 250, added: 250, writes: 1, invalid: [], skipped: [] });
        assert.equal(requests, 3);
        const contents = fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8");
        const second = await main({ directory, reconcile: true, request });
        assert.equal(second.added, 0);
        assert.equal(second.writes, 0);
        assert.equal(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8"), contents);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("failed pagination leaves the journal unchanged", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    const before = fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8");
    const request = async uri => new URL(uri).searchParams.get("page") === "1"
        ? { ok: true, json: async () => Array.from({ length: 100 }, (_, i) => pull(i + 1)) }
        : { ok: false, status: 503, headers: new Headers() };
    try {
        await assert.rejects(main({ directory, reconcile: true, request, sleep: async () => {} }), /HTTP 503/);
        assert.equal(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8"), before);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("release reconciliation excludes PRs outside the packaged revision", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    const request = async uri => ({ ok: true, json: async () => uri.includes("/pulls?")
        ? [pull(1), pull(2)] : { status: uri.includes("merge1...") ? "ahead" : "behind" } });
    try {
        const result = await main({ directory, reconcile: true, snapshot: "release", request });
        assert.equal(result.added, 1);
        assert.equal(yaml.load(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8")).Entries[0].prNumber, 1);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("GET retries are bounded and recover transient GitHub failures", async () => {
    let calls = 0;
    const result = await github("/test", { sleep: async () => {}, request: async () => ++calls < 3
        ? { ok: false, status: 503, headers: new Headers() }
        : { ok: true, json: async () => ({ recovered: true }) } });
    assert.equal(calls, 3);
    assert.deepEqual(result, { recovered: true });
});

test("a truncated successful GitHub response is retried without losing the batch", async () => {
    let calls = 0;
    const result = await github("/test", { sleep: async () => {}, request: async () => ({
        ok: true, json: async () => { if (++calls < 2) throw new SyntaxError("Truncated JSON"); return [pull(1)]; },
    }) });
    assert.equal(calls, 2);
    assert.equal(result[0].number, 1);
});

test("late recovery restores merge order without changing read IDs or depending on a fixed PR list", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    const recent = { ...pull(15), merged_at: "2026-11-08T00:00:00Z" };
    const older = { ...pull(999), merged_at: "2026-11-05T00:00:00Z" };
    const newest = { ...pull(4), merged_at: "2026-11-10T00:00:00Z" };
    let prs = [recent];
    const request = async () => ({ ok: true, json: async () => prs });
    try {
        await main({ directory, reconcile: true, request });
        prs = [newest, recent, older];
        const recovered = await main({ directory, reconcile: true, request });
        assert.equal(recovered.added, 2);
        const entries = yaml.load(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8")).Entries;
        assert.deepEqual(entries.map(e => e.prNumber), [999, 15, 4]);
        assert.deepEqual(entries.map(e => e.id), [2, 1, 3]);
        assert.equal((await main({ directory, reconcile: true, request })).writes, 0);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("template placeholders are rejected and following Markdown sections are excluded", () => {
    assert.throws(() => getChanges("- add: Added fun!"), /template/);
    assert.throws(() => getChanges("- add: Добавлено веселье."), /template/);
    assert.deepEqual(getChanges("- add: Реальное изменение\n  Подробность\n## Summary by bot\n- fix: Example"), {
        ARCANE: [{ type: "Add", message: "Реальное изменение\nПодробность" }],
    });
});

test("missing, commented and empty CLs are reported without creating game or Discord entries", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    process.env.GITHUB_STEP_SUMMARY = path.join(directory, "summary.md");
    const prs = [pull(98, null), pull(101, "<!--\n:cl:\n- add: Hidden\n-->"), pull(102, ":cl:"), pull(103),
        pull(104, "<!--\n:cl:\n- add: Hidden until EOF"),
        pull(105, "    :cl:\n    - add: Indented example"),
        pull(106, "\t:cl:\n\t- add: Tab-indented example"),
        pull(107, "```text\n:cl:\n- add: Fenced example\n```")];
    const request = async () => ({ ok: true, json: async () => prs });
    try {
        const result = await main({ directory, reconcile: true, request });
        assert.equal(result.added, 1);
        assert.deepEqual(result.skipped.map(p => p.number), [98, 101, 102, 104, 105, 106, 107]);
        assert.deepEqual(result.invalid, [102]);
        const entries = yaml.load(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8")).Entries;
        assert.deepEqual(entries.map(e => e.prNumber), [103]);
        const report = fs.readFileSync(process.env.GITHUB_STEP_SUMMARY, "utf8");
        assert.match(report, /pull\/98/);
        assert.match(report, /закомментирован/);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("comments and fences cannot hide the visible CL or expose example media", () => {
    const examples = [
        "```html\n<!--\n:cl:\n- add: Example\nhttps://example.org/hidden.png\n```",
        "<!--\n```\n:cl:\n- add: Example\nhttps://example.org/hidden.png\n-->",
        "    :cl:\n    - add: Indented example",
    ];
    for (const example of examples) {
        const body = "## Media\n" + example + "\n\n## Media\nhttps://example.org/visible.png\n"
            + "   :cl: Author\n- add: Visible change\n  Preserved continuation\n<!-- hidden through EOF\n- fix: Hidden";
        const parsed = parsePullRequest(pull(108, body), "ArcaneSS14/arcane-new");
        assert.equal(parsed.length, 1);
        assert.deepEqual(parsed[0].entry.changes, [{ type: "Add", message: "Visible change\nPreserved continuation" }]);
        assert.deepEqual(parsed[0].entry.media, ["https://example.org/visible.png"]);
    }
});

test("inherited duplicate IDs and URLs preserve history without blocking a batch or creating new duplicates", async () => {
    const directory = fixture();
    const env = { ...process.env };
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    process.env.CHANGELOG_BRANCH = "master";
    const historical = (id, url) => ({ id, url, author: "Upstream", time: "2026-05-05T00:00:00Z",
        changes: [{ type: "Tweak", message: "Historical change " + id }] });
    const trauma = [historical(918, "https://github.com/Trauma-Station/Trauma-Station/pull/2420"),
        historical(919, "https://github.com/Trauma-Station/Trauma-Station/pull/2420")];
    const admin = [historical(5, "https://github.com/space-wizards/space-station-14/pull/1"),
        historical(5, "https://github.com/space-wizards/space-station-14/pull/2")];
    fs.writeFileSync(path.join(directory, "TraumaChangelog.yml"), yaml.dump({ Entries: trauma }));
    fs.writeFileSync(path.join(directory, "Admin.yml"), yaml.dump({ Entries: admin }));
    const prs = [pull(500), pull(501, ":cl:\nTRAUMA:\n- fix: New Trauma change"),
        pull(502, ":cl:\nWIZDENADMIN:\n- fix: New admin change")];
    const request = async uri => ({ ok: true, json: async () => uri.includes("/compare/") ? { status: "ahead" } : prs });
    try {
        const first = await main({ directory, reconcile: true, snapshot: "build", request });
        assert.equal(first.added, 3);
        assert.equal(first.writes, 3);
        for (const [file, original, nextId] of [["TraumaChangelog.yml", trauma, 920], ["Admin.yml", admin, 6]]) {
            const entries = yaml.load(fs.readFileSync(path.join(directory, file), "utf8")).Entries;
            assert.deepEqual(entries.slice(0, 2), original);
            assert.equal(entries.length, 3);
            assert.equal(entries[2].id, nextId);
        }
        const replay = await main({ directory, reconcile: true, snapshot: "build", request });
        assert.equal(replay.added, 0);
        assert.equal(replay.writes, 0);
    } finally {
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});
test("multiple visible CL blocks reject the whole PR while reconciliation recovers other PRs", async () => {
    const directory = fixture();
    const env = { ...process.env };
    const error = console.error;
    const warnings = [];
    process.env.GITHUB_REPOSITORY = "ArcaneSS14/arcane-new";
    process.env.CHANGELOG_BRANCH = "master";
    console.error = message => warnings.push(message);
    const prs = [pull(600, ":cl: First\n- add: Must not publish partially\n🆑 Second\n- fix: Second change"), pull(601)];
    const request = async () => ({ ok: true, json: async () => prs });
    try {
        const result = await main({ directory, reconcile: true, request });
        assert.deepEqual(result.invalid, [600]);
        assert.equal(result.added, 1);
        const entries = yaml.load(fs.readFileSync(path.join(directory, "ArcaneChangelog.yml"), "utf8")).Entries;
        assert.deepEqual(entries.map(entry => entry.prNumber), [601]);
        assert.match(warnings.join("\n"), /PR #600:.*one.*CL.*combine/i);
    } finally {
        console.error = error;
        process.env = env;
        fs.rmSync(directory, { recursive: true });
    }
});

test("media ignores balanced inline code but keeps real Markdown links and images", () => {
    const body = ["## Media", "Example: `https://example.org/example.png`.",
        "Example: ``https://example.org/example2.png with a ` inside``.",
        "Multiline: `example\nhttps://example.org/example3.png`.",
        "![Screenshot](https://example.org/image.png)", "[Video](https://example.org/video.mp4)",
        "Unmatched delimiter: ` https://example.org/visible.png"].join("\n");
    assert.deepEqual(getMedia(body), ["https://example.org/image.png", "https://example.org/video.mp4", "https://example.org/visible.png"]);
});
// Arcane-End
