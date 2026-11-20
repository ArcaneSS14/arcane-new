// Arcane-Start: PR media belongs to the Media section, never examples or hidden comments.
function visibleMarkdown(body) {
    let fence = null;
    let comment = false;
    return (body || "").split(/\r?\n/).map(line => {
        const marker = /^ {0,3}(`{3,}|~{3,})(.*)$/.exec(line);
        if (fence !== null) {
            if (marker && marker[1][0] === fence[0] && marker[1].length >= fence.length && !marker[2].trim()) fence = null;
            return "";
        }
        // Fences in comments and comment examples in fences cannot change the other state.
        if (!comment && marker && (marker[1][0] !== "`" || !marker[2].includes("`"))) {
            fence = marker[1];
            return "";
        }
        let visible = "";
        let cursor = 0;
        while (cursor < line.length) {
            if (comment) {
                const end = line.indexOf("-->", cursor);
                if (end < 0) break;
                comment = false;
                cursor = end + 3;
            } else {
                const start = line.indexOf("<!--", cursor);
                if (start < 0) {
                    visible += line.slice(cursor);
                    break;
                }
                visible += line.slice(cursor, start);
                comment = true;
                cursor = start + 4;
            }
        }
        return visible;
    }).join("\n");
}

function withoutInlineCode(body) {
    const runs = [...body.matchAll(/`+/g)];
    const next = new Array(runs.length);
    const lengths = new Map();
    // Index matching delimiters once, including multiline spans, instead of rescanning suffixes.
    for (let i = runs.length - 1; i >= 0; i--) {
        next[i] = lengths.get(runs[i][0].length);
        lengths.set(runs[i][0].length, i);
    }
    const visible = [];
    let cursor = 0;
    for (let i = 0; i < runs.length; i++) {
        let slashes = 0;
        for (let j = runs[i].index - 1; j >= 0 && body[j] === "\\"; j--) slashes++;
        if (slashes % 2 || next[i] === undefined) continue;
        const end = runs[next[i]].index + runs[next[i]][0].length;
        visible.push(body.slice(cursor, runs[i].index), body.slice(runs[i].index, end).replace(/[^\n]/g, " "));
        cursor = end;
        i = next[i];
    }
    visible.push(body.slice(cursor));
    return visible.join("");
}

function getMedia(body) {
    const lines = withoutInlineCode(visibleMarkdown(body)).split("\n");
    const urls = new Set();
    let section = 0;
    for (const line of lines) {
        const heading = /^ {0,3}(#{1,6})\s+(.+?)\s*#*\s*$/.exec(line);
        if (heading) {
            if (heading[1].length <= section) section = 0;
            if (/^(Медиа|Media)$/i.test(heading[2])) section = heading[1].length;
        }
        if (/^\s*(?::cl:|🆑|\*\*Изменения\*\*)/.test(line)) section = 0;
        if (!section) continue;
        for (const match of line.matchAll(/https?:\/\/[^\s<>"'`]+/gi)) {
            let candidate = match[0].replace(/&amp;/g, "&").replace(/[.,;!]+$/, "");
            for (const [open, close] of [["(", ")"], ["[", "]"]]) {
                const excess = candidate.split(close).length - candidate.split(open).length;
                let end = candidate.length;
                while (candidate.length - end < excess && candidate[end - 1] === close) end--;
                candidate = candidate.slice(0, end);
            }
            try {
                const url = new URL(candidate);
                if (url.hostname && !url.username && !url.password) urls.add(url.href);
            } catch { /* Ignore malformed links in user-authored Markdown. */ }
        }
    }
    return [...urls];
}

module.exports = { getMedia, visibleMarkdown };
// Arcane-End
