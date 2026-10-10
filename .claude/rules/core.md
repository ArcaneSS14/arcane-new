
Read `/AGENTS.md`, the nearest scoped `AGENTS.md`, relevant `.agents/rules`, and task-specific `.agents/skills`.

Do not reproduce those documents here. Use this adapter only to ensure Claude loads the canonical Arcane guidance.

Edit markers, non-negotiable, because these channels do not load `.agents/rules` on their own:

- One added line: trailing `# Arcane` or `// Arcane`, never `Arcane-Start`.
- Two or more added lines: `# Arcane-Start` / `# Arcane-End`, `// Arcane-Start` / `// Arcane-End`.
- One changed line: trailing `# Arcane-Edit: <old> > <new>` or `// Arcane-Edit: <old> > <new>`.
- Two or more changed lines: `# Arcane-Edit-Start` / `# Arcane-Edit-End`, `// Arcane-Edit-Start` / `// Arcane-Edit-End`.
- More than 5 changed lines: keep the changed content active inside the `Arcane-Edit-Start` / `Arcane-Edit-End` block; comment out removed content only when disabling it is intended.
- Never put a bare `-Start` or `-End` on a line instead of a pair.
- Never write `Trauma - `, `<Trauma>`, `Goobstation-`, `/* Trauma`, or any other fork's marker.
- No marker inside Arcane owner-local paths: `Modules/Arcane/**`, `Content.Arcane.*`, `Resources/**/_Arcane/**`, and `Resources/Locale/**/_Arcane/**`. Nothing syncs there.
- Mark Arcane changes inside `Content.Trauma.*`, `Resources/_Trauma/**`, `*.Trauma.cs`, `Content.Medical.*`, `Resources/_Shitmed/**`, and vanilla root paths. Editing a Trauma file is allowed and often correct; an unmarked change there is reverted by the next sync.
- Changing a line that carries an upstream marker makes it ours: `# Arcane-Edit: 1800 > 3000`. Leave upstream markers on untouched lines alone.
- Keep Trauma-file edits cheap: append to the end of a list, gather additions into one block, prefer additive over destructive, do not touch neighbours.

Canonical text: `.agents/rules/arcane-edit-markers.md`, `.agents/rules/fork-trajectory-priority.md`, `.agents/rules/merge-conflict-resolution.md`.

Prefer root `Content.Arcane.*` projects and existing `_Arcane` resource paths for Arcane-only work and ports. Follow `.agents/rules/port-destination.md` for the provenance-based exception for confirmed TraumaStation additions and the separate rule for restoring behavior removed by a Trauma-side revert. Any necessary inherited/base integration change must be minimal and marked.

Sync resolution: a conflicted line gets the marker of whoever wrote the resolved line. Our resolution is ours, so it carries an Arcane marker. Do not blanket-`git checkout --ours/--theirs`, do not pass `-X ours`/`-X theirs`, and never `git add -u` a path you did not read.
