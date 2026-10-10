
# Rule and skill routing

Read the root `AGENTS.md` and the nearest scoped `AGENTS.md` files first. For cross-layer work, ownership/build changes, or another non-routine workflow, consult the matching entry in `.agents/SCENARIOS.md`. Use this catalog to load only the rule files and skills for surfaces actually affected.

Rules are repository policy; skills are task playbooks. A selected skill is an execution contract for its topic. Do not read every rule or skill by default. A cross-layer task can legitimately need several skills.

## Rules by situation

- Any task: root and nearest scoped `AGENTS.md`; use `CONTRIBUTING.md` for contributor-facing project conventions.
- Ownership, project references, or module placement: `.agents/rules/architecture-and-ownership.md`; read `.agents/skills/module-architecture/SKILL.md` when assembly or ownership analysis is needed.
- Inherited file or upstream sync choice: `.agents/rules/fork-trajectory-priority.md` and `.agents/rules/arcane-edit-markers.md`.
- Reverts, restorations, cherry-picks/backports, ports, or unexplained removals: `.agents/rules/change-history-analysis.md`; add domain rules and skills for affected code and resources.
- Actual merge conflict: add `.agents/rules/merge-conflict-resolution.md`.
- C# layout, comments, ECS, or YAML format: read only the matching `.agents/rules/*conventions.md` file.
- Localization or player-visible text: `.agents/rules/content-and-localization.md` and the relevant localization skill.
- API design and access boundary: `.agents/rules/coding-and-api-design.md`.
- Third-party code or assets: `.agents/rules/third-party-materials.md`.
- Verification or handoff: `.agents/rules/verification.md` and `.agents/rules/review-and-handoff.md` when applicable.

## Skills by changed surface

- Local C# implementation: `csharp-style`; add `module-architecture` for project boundaries and `testing` when choosing checks.
- ECS implementation: choose the relevant `ecs-*`, entity API, lifecycle, interaction, or domain skill.
- Client/server/shared, network state, prediction, or trust boundary: `client-server-shared` plus only the applicable `networking`, `prediction`, `security-and-validation`, and domain skills.
- UI: `xaml-ui` for XAML controls/code-behind; `bound-user-interface` for entity-owned UI contracts and message/state flow (use both when both surfaces change); `eui` for session-oriented interfaces. Add input validation and localization skills only when those surfaces change.
- Localization: `localization`; add `localization-in-code` when code resolves text.
- Prototypes and YAML: `prototypes` or `yaml-and-schema`; add `prototype-localization` when visible prototype text changes.
- Maps and assets: `maps-and-mapping` and/or `resources-and-assets`, plus the affected format or domain skill.
- Build, project, CI, or packaging: `build-and-packaging`; add `module-architecture` when project ownership or references change.
- Tests: `tests-authoring` when writing tests; `testing` when choosing or running checks.
- Complete feature spanning layers: `gameplay-feature` plus the technical skills for the affected layers.
- Feature port: `porting` and `upstream-maintenance`, plus the domain skills for ported code and resources.
- Review: `code-review` plus the technical skills for the changed surfaces.

## Always-on invariants

- Arcane runtime projects are root-level `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client`. Arcane resources use existing `Resources` owner directories such as `Resources/Prototypes/_Arcane`, `Resources/Textures/_Arcane`, and `Resources/Locale/{culture}/_Arcane`. `Modules/Arcane` currently contains guidance, not runtime projects.
- Arcane-owned projects and `_Arcane` owner directories do not need Arcane edit markers. Inherited paths on the TraumaStation sync surface do; consult the marker rule for language-specific syntax and line-count details.
- Never author our change under another fork's marker. Preserve upstream markers on lines we did not change.
- `en-US` is the structural source of truth. Mirror structural localization changes in the matching `ru-RU` owner path, creating a counterpart when needed; preserve keys, attributes, variables, selectors, paths, and ordering.
- Do not cross engine boundaries, invent APIs, bypass access modifiers, or expand repository searches beyond the task's evidence needs.
