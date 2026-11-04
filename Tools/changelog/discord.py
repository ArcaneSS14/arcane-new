# Arcane-Start
"""Russian Discord cards for the Arcane journal (Discord Components V2).

Behavior adapted from ArcaneSS14/arcane: per-PR cards and media galleries.
The renderer is offline: media URLs are resolved by Discord, never by the runner.
"""

import re
from datetime import datetime
from urllib.parse import urlsplit, quote


TYPES = {
    "Add": ("🆕 Добавлено", 0x6ED18D),
    "Fix": ("🐛 Исправлено", 0xD1BA6E),
    "Tweak": ("🔧 Изменено", 0x6E96D1),
    "Remove": ("🗑️ Удалено", 0xD16E6E),
}
ACCENT = 0x9882CE
TEXT_BUDGET = 3000  # Leave room for the repeated heading and footer.


def text_length(value):
    return len(value.encode("utf-16-le")) // 2


def split_text(value, budget):
    """Preserve every character, including a single overlong change line."""
    if budget < 2:
        raise ValueError("The text budget must fit a Unicode code point")
    buffer = []
    size = 0
    for char in value:
        units = 2 if ord(char) > 0xFFFF else 1
        if size + units > budget:
            text = "".join(buffer)
            newline = text.rfind("\n")
            end = newline + 1 if newline > len(text) // 2 else len(text)
            yield text[:end]
            tail = text[end:]
            buffer = list(tail)
            size = text_length(tail)
        buffer.append(char)
        size += units
    if buffer:
        yield "".join(buffer)


def safe_url(value):
    if not isinstance(value, str) or any(ord(char) < 32 for char in value):
        return None
    try:
        parsed = urlsplit(value.strip())
        if parsed.scheme not in ("http", "https") or not parsed.hostname or parsed.username or parsed.password:
            return None
        parsed.port  # Reject malformed ports as well.
        return quote(value.strip(), safe=":/?#[]@!$&'*+,;=%~_-.")
    except ValueError:
        return None


def escape_text(value):
    return re.sub(r"([\\`*_~|<>\[\]])", r"\\\1", str(value)).replace("@", "@\u200b")


def media_urls(entry):
    values = entry.get("media") or []
    if not isinstance(values, list):
        return []
    return list(dict.fromkeys(url for value in values if (url := safe_url(value)) and len(url) <= 2000))


def render_entry(entry):
    url = safe_url(entry.get("url"))
    pr = re.fullmatch(r"https://github\.com/[\w.-]+/[\w.-]+/pull/(\d+)/?", url or "")
    title = "История Arcane" if entry.get("historical") else "Изменения Arcane"
    heading = f"## {title} · [PR #{pr[1]}]({url})" if pr else "## " + title
    author = escape_text(str(entry.get("author") or "Неизвестный автор")[:120])
    details = f"-# Автор: **{author}**"
    try:
        date = datetime.fromisoformat(str(entry.get("time")))
        if date.tzinfo is not None:
            details += f" · <t:{int(date.timestamp())}:d>"
    except (ValueError, TypeError, OverflowError):
        pass

    grouped = {}
    for change in entry.get("changes") or []:
        kind = change.get("type")
        key = kind if kind in TYPES else "Other"
        grouped.setdefault(key, []).append("• " + escape_text(change.get("message", "")))
    sections = []
    for kind in [*TYPES, "Other"]:
        if kind in grouped:
            label = TYPES.get(kind, ("📝 Прочее", ACCENT))[0]
            sections.append(f"### {label}\n" + "\n".join(grouped[kind]))
    if "Intent: Experimental" in (entry.get("labels") or []):
        sections.insert(0, "🧪 **Экспериментальные изменения**")
    body = "\n\n".join(sections) or "Нет описания изменений."
    pages = list(split_text(body, TEXT_BUDGET))
    links = media_urls(entry)
    galleries = [links[i:i + 10] for i in range(0, len(links), 10)]
    count = max(len(pages), len(galleries))
    color = TYPES[next(iter(grouped))][1] if len(grouped) == 1 and next(iter(grouped)) in TYPES else ACCENT
    for index in range(count):
        title = heading if index == 0 else heading + " · продолжение"
        components = [
            {"type": 10, "content": title + "\n" + details},
            {"type": 14, "divider": True, "spacing": 1},
        ]
        if index < len(pages):
            components.append({"type": 10, "content": pages[index]})
        if index < len(galleries):
            components.append({"type": 12, "items": [
                {"media": {"url": item}, "description": "Медиа к изменению"}
                for item in galleries[index]
            ]})
        footer = "Arcane Station · Журнал изменений"
        if count > 1:
            footer += f" · {index + 1}/{count}"
        components.append({"type": 10, "content": "-# " + footer})
        yield {
            "flags": 1 << 15,
            "allowed_mentions": {"parse": []},
            "components": [{"type": 17, "accent_color": color, "components": components}],
        }


def link_fallback(payload):
    """A rejected gallery becomes links; retain the text without truncating it."""
    card = payload["components"][0]
    components = card["components"]
    links = [item["media"]["url"] for component in components if component["type"] == 12 for item in component["items"]]
    if not links:
        return []
    result = [{**payload, "components": [{**card, "components": [c for c in components if c["type"] != 12]}]}]
    for text in split_text("### Медиа к изменению\n" + "\n".join(f"<{url}>" for url in links), TEXT_BUDGET):
        result.append({**payload, "components": [{**card, "components": [components[0], {"type": 10, "content": text}]}]})
    return result
# Arcane-End
