#!/usr/bin/env python3
import json
import re
import subprocess
import sys
from pathlib import Path

ARCANE_OWNED = ("Modules/Arcane/", "Content.Arcane.")
INFRASTRUCTURE = (".agents/", ".claude/", ".cursor/", ".codex/", "AGENTS.md", "CLAUDE.md")
MARKER = re.compile(r"Arcane(?:-Edit|-Start|-Edit-Start|-End|-Edit-End)?\b")
FOREIGN = re.compile(r"(?:Trauma\s*-\s|<Trauma>|</Trauma>|Goobstation\s*-\s|<Goob>|/\*\s*Trauma)")
# A `-Start` marker is only legal as a whole comment line. Trailing after code it means
# a single-line change was wrapped instead of marked inline.
TRAILING_START = re.compile(
    r"^.*\S\s(?:#|//)\s*Arcane-(?:Edit-)?Start(?:\s*:.*)?$"
)


def git(*args: str) -> str:
    result = subprocess.run(
        ["git", *args], capture_output=True, text=True, check=False
    )
    return result.stdout


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        payload = {}

    findings: list[str] = []

    raw = git("diff", "--unified=0")
    current = ""
    for line in raw.split("\n"):
        if line.startswith("+++ b/"):
            current = line[6:]
            continue
        if not line.startswith("+") or line.startswith("+++"):
            continue
        added = line[1:]
        if added.strip() == "":
            continue
        if current.endswith((".md", ".yml", ".yaml", ".toml")):
            continue
        arcane_owned = any(current.startswith(p) for p in ARCANE_OWNED) or (
            current.startswith("Resources/") and "_Arcane" in current.split("/")
        )
        if arcane_owned:
            if MARKER.search(added):
                findings.append(f"marker inside Arcane owner-local path: {current}: {added.strip()[:70]}")
            continue
        if any(current.startswith(p) for p in INFRASTRUCTURE):
            continue
        if FOREIGN.search(added) and not MARKER.search(added):
            findings.append(f"foreign marker on an added line: {current}: {added.strip()[:70]}")
        if TRAILING_START.match(added.rstrip()):
            findings.append(
                f"trailing -Start on a single line, use inline Arcane or Arcane-Edit: "
                f"{current}: {added.strip()[:70]}"
            )

    untracked = [
        p for p in git("ls-files", "--others", "--exclude-standard").split("\n") if p
    ]

    parts = [
        "Before finishing: inspect git status and diff, report exact checks, state unrun checks,",
        "and verify whether any commit was actually pushed.",
    ]
    if untracked:
        parts.append(f"Untracked files ({len(untracked)}): {', '.join(untracked[:12])}")
    if findings:
        parts.append("")
        parts.append("MARKER CHECK FAILED:")
        parts.extend(f"- {f}" for f in findings[:20])
    elif git("diff", "--check"):
        parts.append("`git diff --check` is not clean.")
    else:
        parts.append("Marker pre-check found no foreign marker on added lines and no marker in Arcane-owned paths.")

    json.dump({"additionalContext": "\n".join(parts), "input": payload}, sys.stdout)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
