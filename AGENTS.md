# Repository agent guidance

Work from the paths, symbols, resources or failure named in the current request. Search the likely owner first; expand only to resolve a concrete missing dependency. Do not inventory the repository or load every rule/skill for routine work. An explicitly requested instruction audit may inspect its relevant instruction set.

## Select guidance

1. Follow higher-priority instructions and the user's current request, then root and nearest scoped AGENTS.md.
2. Use [.agents/CATALOG.md](.agents/CATALOG.md) for the changed surfaces and [CONTRIBUTING.md](CONTRIBUTING.md) for contributor conventions. Rules set project policy; skills explain its application.
3. Use [.agents/SCENARIOS.md](.agents/SCENARIOS.md) when several layers, history, conflicts or build structure interact. Load only the relevant combination; there is no fixed skill count.
4. For root Content.* code, find the owner/project guide through [Modules/CATALOG.md](Modules/CATALOG.md). Modules/ contains guidance, not relocated runtime projects; do not assume a module.yml exists.

Resolve an instruction conflict by priority and current task scope. Ask about a material unresolved behavior or authority decision; do not silently replace project policy with a client adapter or optional external library.

## Boundaries

- Preserve user changes and approved behavior. Reuse the existing system, owner, project, resource root and test infrastructure. Verify unfamiliar declarations, access and actual consumers before using them.
- Arcane runtime lives in root Content.Arcane.*; Arcane resources use existing _Arcane owner directories. [Ownership](.agents/rules/architecture-and-ownership.md) and [trajectory](.agents/rules/fork-trajectory-priority.md) decide placement, including Trauma sync-sensitive paths.
- In inherited code/resources, apply [Arcane markers](.agents/rules/arcane-edit-markers.md) to our changes only. Do not reformat neighbours or convert untouched foreign markers. Resolve actual conflicts under [merge policy](.agents/rules/merge-conflict-resolution.md), preserving confirmed Arcane intent. Sprite/map conflicts go to the user.
- Addressed engine source reads and CI's read-only content validators are allowed. Engine edits, commands inside the submodule and gitlink updates need a current explicit request: [engine boundary](.agents/rules/engine-boundaries.md).
- Preserve SPDX lines unless the current request specifies their exact change. Instruction files under .agents/, .claude/, .cursor/ and .codex/ carry no SPDX headers. Git mutations and existing work follow [Git safety](.agents/rules/git-safety.md); commit/push permission does not authorize merge.
- Use [localization policy](.agents/rules/content-and-localization.md): en-US by default; ru-RU only for explicitly requested Russian localization or the reported Russian-specific defect. Do not automatically synchronize it.
- Third-party code, assets and documentation follow [provenance and rights](.agents/rules/third-party-materials.md). Public contribution and CI must work without any private instruction package.

## Verify and deliver

Start with the smallest sufficient check and expand for a concrete uncovered risk, up to a full set when necessary. Respect current restrictions on where and how checks run; GitHub validation does not authorize a duplicate full local run. Use [verification.md](.agents/rules/verification.md) for commands and owners.

For documentation/instruction edits, inspect the diff and affected links/routes; exercise a changed executable helper separately. Do not build the game for Markdown alone. Do not automatically restore dependencies, update submodules or run the whole solution/test suite.

Before completion, inspect the final task diff, run git diff --check and git diff --stat, and report the change, actual checks and material omissions under [handoff](.agents/rules/review-and-handoff.md). Do not claim runtime, visual or server-side results from static inspection.
