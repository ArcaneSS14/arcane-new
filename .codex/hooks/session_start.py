#!/usr/bin/env python3
import json
import sys
from pathlib import Path

RULES = Path(".agents/rules")


def load(name: str) -> str:
    path = RULES / name
    if not path.is_file():
        return ""
    return path.read_text(encoding="utf-8", errors="ignore").strip()


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        payload = {}

    parts = [
        "Repository: Arcane, a fork of TraumaStation. TraumaStation is the sync source, so inherited"
        " and Trauma-owned files are the conflict surface. Prefer Content.Trauma.* for upstream-compatible"
        " behavior and Modules/Arcane for Arcane-only behavior.",
        "",
        "EDIT MARKERS, APPLIED VERBATIM. Every change we author carries Arcane markers only.",
        "Never write Trauma - , <Trauma>, Goobstation-, /* Trauma, or any other fork's marker.",
        "No marker inside Arcane owner-local paths: Modules/Arcane/**, Content.Arcane.*, Resources/_Arcane/**.",
        "Required inside Content.Trauma.*, Resources/_Trauma/**, *.Trauma.cs, Content.Medical.*,",
        "Resources/_Shitmed/**, and vanilla root paths. Editing a Trauma file is allowed and often correct.",
        "A line we change becomes ours, so an upstream marker becomes Arcane-Edit: <old> > <new>.",
        "Leave upstream markers on untouched lines alone.",
        "Keep Trauma-file edits cheap: append to the end of a list, gather additions into one block,",
        "prefer additive over destructive, do not reformat or reorder neighbours.",
        "",
        "Read AGENTS.md, the nearest scoped AGENTS.md, and the task-matching .agents/skills before editing.",
        "English localization is structural truth; mirror Russian keys, variables, selectors, paths, and order.",
    ]

    for name in ("arcane-edit-markers.md", "fork-trajectory-priority.md"):
        body = load(name)
        if body:
            parts += ["", f"--- .agents/rules/{name} ---", body]

    json.dump({"additionalContext": "\n".join(parts), "input": payload}, sys.stdout)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
