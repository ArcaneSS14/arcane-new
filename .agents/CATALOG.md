
Read root `AGENTS.md` and the nearest scoped `AGENTS.md` first. Select only skills required by changed surfaces.

Every selected skill is an execution contract. Follow its discovery, ownership, edit-marker, localization, stop, and verification rules.

## Mandatory routing

- Repository identity, inherited files, modules, or underscore owner paths: `module-architecture`, `upstream-maintenance`, `git-workflow`.
- Editing a Trauma, vanilla, Medical, or Shitmed file, choosing where a change belongs, or resolving a sync conflict: `.agents/rules/fork-trajectory-priority.md`, `.agents/rules/arcane-edit-markers.md`, `.agents/rules/merge-conflict-resolution.md`.

Always-on invariants, no skill needed:

- Our own changes carry Arcane markers only. Never `Trauma - `, `<Trauma>`, or another fork's marker.
- One added line is a bare trailing `# Arcane`. Two or more added lines use `# Arcane-Start` / `# Arcane-End`.
- One changed line is `# Arcane-Edit: <old> > <new>`. Two or more changed lines use `# Arcane-Edit-Start` / `# Arcane-Edit-End`.
- Never put a bare `-Start` or `-End` on a line instead of a pair.
- No marker inside `Modules/Arcane/**`, `Content.Arcane.*`, `Resources/_Arcane/**`.
- A change to an upstream-marked line becomes ours, so its marker becomes `Arcane-Edit`.
- Editing a Trauma file is allowed and often correct; keep it cheap to reconcile.
- English localization is structural truth; Russian mirrors key, variable, selector, path, and message order.
- C# or project references: `csharp-style`, `module-architecture`, `testing`.
- Shared, networking, prediction, or authority: `client-server-shared`, `networking`, `prediction`, `tests-authoring`.
- FTL or player-visible text: `localization`, `localization-in-code`, `testing`.
- Prototypes with display text: `prototypes`, `prototype-localization`, `localization`, `yaml-and-schema`.
- XAML or BUI: `xaml-ui`, `bound-user-interface`, `forms-and-input-validation`, `localization`, `testing`.
- Assets or maps: `resources-and-assets`, `maps-and-mapping`, `yaml-and-schema`, `testing`.
- Project, solution, CI, MSBuild, or packaging: `module-architecture`, `build-and-packaging`, `testing`.
- Ports: `porting`, `upstream-maintenance`, and every domain skill matching ported files.

`en-US` is the structural localization source of truth. Any localization task must preserve Russian key, attribute, variable, selector, file-path, and message-order parity.
