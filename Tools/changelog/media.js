// Arcane-Start: PR media belongs to the Media section, never examples or hidden comments.
function withoutCodeBlocks(body) {
    let fence = null;
    return body.split(/\r?\n/).map(line => {
        const marker = /^ {0,3}(`{3,}|~{3,})(.*)$/.exec(line);
        if (marker) {
            if (fence === null) fence = marker[1];
            else if (marker[1][0] === fence[0] && marker[1].length >= fence.length && !marker[2].trim()) fence = null;
            return "";
        }
        return fence === null ? line : "";
    }).join("\n");
}

function getMedia(body) {
    const lines = withoutCodeBlocks((body || "").replace(/<!--[\s\S]*?-->/g, "")).split("\n");
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

module.exports = { getMedia, withoutCodeBlocks };
// Arcane-End
