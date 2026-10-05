
Read `/AGENTS.md`, the nearest scoped `AGENTS.md`, relevant `.agents/rules`, and task-specific `.agents/skills`.

Do not reproduce those documents here. Use this adapter only to ensure Claude loads the canonical Arcane guidance.

Edit markers, non-negotiable, because these channels do not load `.agents/rules` on their own:

- Our own changes carry Arcane markers only: `// Arcane-Edit: <old> > <new>`, `// Arcane-Start` / `// Arcane-End`, `# Arcane-Edit`, `# Arcane-Start` / `# Arcane-End`. Never `Trauma - `, `<Trauma>`, `Goobstation-`, `/* Trauma`, or any other fork's marker.
- No marker inside Arcane owner-local paths: `Modules/Arcane/**`, `Content.Arcane.*`, `Resources/_Arcane/**`. Nothing syncs there.
- Mark Arcane changes inside `Content.Trauma.*`, `Resources/_Trauma/**`, `*.Trauma.cs`, `Content.Medical.*`, `Resources/_Shitmed/**`, and vanilla root paths. Editing a Trauma file is allowed and often correct; an unmarked change there is reverted by the next sync.
- Changing a line that carries an upstream marker makes it ours: `# Arcane-Edit: 1800 > 3000`. Leave upstream markers on untouched lines alone.
- Keep Trauma-file edits cheap: append to the end of a list, gather additions into one block, prefer additive over destructive, do not touch neighbours.

Canonical text: `.agents/rules/arcane-edit-markers.md`, `.agents/rules/fork-trajectory-priority.md`, `.agents/rules/merge-conflict-resolution.md`.

Prefer `Modules/Arcane` and `Content.Trauma.*` for new work. For a feature port, `Content.Trauma.*` survives a sync while `Modules/Arcane` does not. Base-file edits are the exception and must be marked.

Sync resolution: a conflicted line gets the marker of whoever wrote the resolved line. Our resolution is ours, so it carries an Arcane marker. Do not blanket-`git checkout --ours/--theirs`, do not pass `-X ours`/`-X theirs`, and never `git add -u` a path you did not read.
