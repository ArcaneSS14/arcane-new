// Arcane-Edit-Start: batched, replay-safe collection of merged PR changelogs.
// The original category-based collector is credited in package.json.
const fs = require("node:fs");
const path = require("node:path");
const yaml = require("js-yaml");
const { getMedia, withoutCodeBlocks } = require("./media");

const CategoryPaths = {
    ARCANE: "ArcaneChangelog.yml",
    WIZDEN: "Changelog.yml",
    WIZDENADMIN: "Admin.yml",
    MAPS: "Maps.yml",
    ADMIN: "TraumaAdmin.yml",
    RULES: "Rules.yml",
    TRAUMA: "TraumaChangelog.yml",
};
const Types = { add: "Add", remove: "Remove", tweak: "Tweak", fix: "Fix", bugfix: "Fix", bug: "Fix" };
const DefaultDirectory = path.resolve(__dirname, "../../Resources/Changelog");
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

function getChanges(body) {
    const entries = {};
    let category = "ARCANE";
    let previous;
    for (const line of body.split(/\r?\n/)) {
        if (/^\s*#{1,6}\s/.test(line) || /^\s*(?::cl:|🆑)/.test(line)) break;
        if (!line.trim()) continue;
        const change = /^\s*[-*]?\s*(add|remove|tweak|fix|bugfix|bug):\s*(.+?)\s*$/i.exec(line);
        if (change) {
            const message = change[2].trim();
            if (/^(?:(Added|Removed|Changed|Fixed) fun!|(Добавлено|Убрано|Изменено|Исправлено) веселье\.)$/i.test(message))
                throw new Error("Replace the template changelog examples with actual changes");
            previous = { type: Types[change[1].toLowerCase()], message };
            (entries[category] ??= []).push(previous);
            continue;
        }
        const header = /^([a-z]+):\s*$/i.exec(line);
        if (header) {
            category = header[1].toUpperCase();
            if (!CategoryPaths[category]) throw new Error("Unknown changelog category: " + category);
            previous = undefined;
            continue;
        }
        if (/^\s{2,}\S/.test(line) && previous) {
            previous.message += "\n" + line.trim();
            continue;
        }
        throw new Error("Invalid changelog line: " + line.slice(0, 160));
    }
    return Object.keys(entries).length ? entries : null;
}

function parsePullRequest(pr, repository) {
    const visible = withoutCodeBlocks((pr.body || "").replace(/<!--[\s\S]*?-->/g, ""));
    const header = /^[ \t]*(?::cl:|🆑)[ \t]*([^\r\n]*)(?:\r?\n([\s\S]*))?$/im.exec(visible);
    if (!header) return null;
    const changes = getChanges(header[2] || "");
    if (!changes) throw new Error("The changelog header has no entries");
    const timestamp = new Date(pr.merged_at);
    if (!Number.isFinite(timestamp.getTime())) throw new Error("Invalid merge date");
    return Object.entries(changes).map(([category, lines]) => ({
        category,
        entry: {
            author: header[1].trim() || pr.user.login,
            changes: lines,
            id: -1,
            time: timestamp.toISOString(),
            url: "https://github.com/" + repository + "/pull/" + pr.number,
            prNumber: pr.number,
            labels: (pr.labels || []).map(label => typeof label === "string" ? label : label.name),
            media: getMedia(pr.body),
        },
    }));
}

class Journals {
    constructor(directory) {
        this.directory = directory;
        this.files = new Map();
    }

    load(category) {
        if (this.files.has(category)) return this.files.get(category);
        const filename = path.join(this.directory, CategoryPaths[category]);
        const raw = fs.readFileSync(filename, "utf8");
        const data = yaml.load(raw);
        if (!data || !Array.isArray(data.Entries)) throw new Error("Invalid journal: " + filename);
        const urls = new Set();
        const ids = new Set();
        let maxId = 0;
        for (const entry of data.Entries) {
            if (!Number.isSafeInteger(entry.id) || ids.has(entry.id)) throw new Error("Invalid or duplicate ID in " + filename);
            ids.add(entry.id);
            maxId = Math.max(maxId, entry.id);
            if (entry.url) {
                if (urls.has(entry.url)) throw new Error("Duplicate PR in " + filename);
                urls.add(entry.url);
            }
        }
        const journal = { filename, data, urls, maxId, changed: false };
        this.files.set(category, journal);
        return journal;
    }

    add(category, entry) {
        const journal = this.load(category);
        if (journal.urls.has(entry.url)) return false;
        entry.id = ++journal.maxId;
        journal.data.Entries.push(entry);
        journal.urls.add(entry.url);
        journal.changed = true;
        return true;
    }

    save() {
        let writes = 0;
        // Serialize everything before touching files; a failed batch is never committed.
        const pending = [...this.files.entries()].filter(([, journal]) => journal.changed).map(([category, journal]) => {
            // Keep merge chronology even when an older PR is recovered later.
            // IDs remain immutable so clients still recognize recovered entries as unread.
            journal.data.Entries.sort((a, b) => new Date(a.time) - new Date(b.time) || (a.prNumber || a.id) - (b.prNumber || b.id));
            let content = yaml.dump(journal.data, { indent: 2, noArrayIndent: true, lineWidth: -1 });
            if (category === "ARCANE") content = "# Arcane-Start\n" + content + "# Arcane-End\n";
            return { ...journal, content };
        });
        for (const journal of pending) {
            const temporary = journal.filename + ".tmp";
            fs.writeFileSync(temporary, journal.content, "utf8");
            fs.renameSync(temporary, journal.filename);
            writes++;
        }
        return writes;
    }
}

async function github(route, { request = fetch, sleep = delay } = {}) {
    const headers = { Accept: "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28" };
    if (process.env.GITHUB_TOKEN) headers.Authorization = "Bearer " + process.env.GITHUB_TOKEN;
    const origin = process.env.GITHUB_API_URL || "https://api.github.com";
    for (let attempt = 0; attempt < 4; attempt++) {
        let response;
        try {
            response = await request(origin + route, { headers, signal: AbortSignal.timeout(30000) });
        } catch {
            if (attempt === 3) throw new Error("GitHub request failed after four attempts");
            await sleep(500 * 2 ** attempt);
            continue;
        }
        if (response.ok) {
            try { return await response.json(); }
            catch {
                if (attempt === 3) throw new Error("GitHub returned an incomplete JSON response after four attempts");
                await sleep(500 * 2 ** attempt);
                continue;
            }
        }
        const limited = response.status === 429 || (response.status === 403 && response.headers?.get("retry-after"));
        if ((limited || response.status >= 500) && attempt < 3) {
            const seconds = Number(response.headers?.get("retry-after") || 2 ** attempt);
            if (!Number.isFinite(seconds) || seconds < 0 || seconds > 60) throw new Error("GitHub retry delay exceeds the retry budget");
            await sleep(Math.max(seconds, 0.1) * 1000);
            continue;
        }
        throw new Error("GitHub request failed (HTTP " + response.status + ")");
    }
}

async function collectPullRequests(repository, branch, options) {
    if (options.input) return JSON.parse(fs.readFileSync(options.input, "utf8"));
    if (!options.reconcile) {
        const number = Number(options.prNumber || process.env.PR_NUMBER);
        if (!Number.isSafeInteger(number) || number < 1) throw new Error("PR_NUMBER is required");
        return [await github("/repos/" + repository + "/pulls/" + number, options)];
    }
    const result = [];
    for (let page = 1; ; page++) {
        // Editing an old PR must not move it across pages while we scan.
        const query = new URLSearchParams({ state: "closed", base: branch, sort: "created", direction: "asc", per_page: "100", page: String(page) });
        const pulls = await github("/repos/" + repository + "/pulls?" + query, options);
        if (!Array.isArray(pulls)) throw new Error("GitHub returned an invalid PR page");
        result.push(...pulls);
        if (pulls.length < 100) break;
    }
    return result;
}

async function main(options = {}) {
    const repository = process.env.GITHUB_REPOSITORY;
    if (!/^[\w.-]+\/[\w.-]+$/.test(repository || "")) throw new Error("GITHUB_REPOSITORY is required");
    const branch = process.env.CHANGELOG_BRANCH || "master";
    const journals = new Journals(options.directory || DefaultDirectory);
    const own = journals.load("ARCANE");
    const curated = new Set(own.data.Entries.filter(e => e.historical).map(e => e.prNumber));
    const excluded = new Set(own.data.ExcludedPullRequests || []);
    const prs = await collectPullRequests(repository, branch, options);
    prs.sort((a, b) => String(a.merged_at).localeCompare(String(b.merged_at)) || a.number - b.number);
    let added = 0;
    const invalid = [];
    const skipped = [];
    for (const pr of prs) {
        if (!pr.merged_at || (pr.base?.ref && pr.base.ref !== branch)) continue;
        if (excluded.has(pr.number)) {
            skipped.push({ number: pr.number, reason: "Явно исключён из журнала" });
            continue;
        }
        if (curated.has(pr.number)) continue;
        let parsed;
        try {
            parsed = parsePullRequest(pr, repository);
        } catch (error) {
            invalid.push(pr.number);
            skipped.push({ number: pr.number, reason: "Некорректный или незаполненный CL" });
            console.error("::warning::PR #" + pr.number + ": " + error.message.replace(/[\r\n]/g, " "));
            continue;
        }
        if (!parsed) {
            skipped.push({ number: pr.number, reason: "Нет видимого блока CL (отсутствует или закомментирован)" });
            continue;
        }
        const missing = parsed.filter(({ category, entry }) => !journals.load(category).urls.has(entry.url));
        if (!missing.length) continue;
        if (options.snapshot) {
            const comparison = await github("/repos/" + repository + "/compare/" + pr.merge_commit_sha + "..." + options.snapshot, options);
            if (!["ahead", "identical"].includes(comparison.status)) continue;
        }
        for (const { category, entry } of missing) if (journals.add(category, entry)) added++;
    }
    const writes = journals.save();
    console.log("Checked " + prs.length + " PRs; added " + added + " entries; wrote " + writes + " journals.");
    for (const item of skipped) console.log("Skipped PR #" + item.number + ": " + item.reason);
    if (process.env.GITHUB_STEP_SUMMARY) {
        const report = ["### Сверка CL Arcane", `Проверено PR: ${prs.length}. Добавлено записей: ${added}. Пропущено: ${skipped.length}.`,
            ...skipped.map(item => `- [PR #${item.number}](https://github.com/${repository}/pull/${item.number}) — ${item.reason}.`), ""];
        fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, report.join("\n") + "\n");
    }
    // An event with a broken CL is visible as a failed run; reconciliation still recovers other PRs.
    if (invalid.length && !options.reconcile && !options.input) throw new Error("Invalid changelog in PR #" + invalid.join(", #"));
    return { checked: prs.length, added, writes, invalid, skipped };
}

module.exports = { main, getChanges, parsePullRequest, Journals, github, collectPullRequests };
if (require.main === module) {
    const args = process.argv.slice(2);
    const value = flag => args.includes(flag) ? args[args.indexOf(flag) + 1] : undefined;
    main({ reconcile: args.includes("--reconcile"), input: value("--input"), snapshot: value("--snapshot") })
        .catch(error => { console.error(error.message); process.exitCode = 1; });
}
// Arcane-Edit-End
