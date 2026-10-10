# Scenario routing

Use this file to identify the workflow for a task. The root `AGENTS.md` defines common constraints; `.agents/CATALOG.md` routes skills; `.agents/rules` contains focused policies. Read only the rule files and skills that apply to the actual changed paths.

## Routine owner-local change

1. Read root guidance and the nearest scoped `AGENTS.md` files.
2. Identify the exact owner from the path and project/resource metadata.
3. Search the narrowest relevant directory for existing behavior and patterns.
4. Select the relevant skills in `.agents/CATALOG.md`.
5. Verify with the smallest check covering the changed files.

Arcane projects live at the repository root as `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client`. Arcane prototypes and textures use the existing `Resources/**/_Arcane` paths. Arcane English locale uses `Resources/Locale/en-US/_Arcane`; add or update `ru-RU/_Arcane` only when the user explicitly requests Russian localization. `Modules/Arcane` currently holds guidance, not runtime projects.

## Inherited or sync-sensitive change

Use this workflow only when a changed path is inherited or lies on the TraumaStation sync surface. Read `architecture-and-ownership.md`, `fork-trajectory-priority.md`, and `arcane-edit-markers.md`; read `merge-conflict-resolution.md` only when resolving an actual conflict. Use `module-architecture`, `upstream-maintenance`, or `git-workflow` when the change also affects their concerns.

- Mark our own changes with Arcane markers only. Never write an upstream fork marker as authorship for our change.
- Match marker comment syntax to the file format. If comments are unsafe or unsupported, do not force a marker into the file; follow the focused marker rule and report the limitation.
- Do not mark Arcane-owned projects or `_Arcane` resource directories.
- Replace an upstream marker only on a line we actually change; preserve markers on untouched lines.
- Keep inherited diffs small and avoid unrelated formatting or neighboring-line edits.

## Revert, restoration, port, or unexplained removal

Read `.agents/rules/change-history-analysis.md` before deciding what behavior to restore, omit, or port. Trace the introduction, follow-up fixes, reverts, and current source/destination state by exact commit and affected paths; do not rely on commit titles alone. For a port, also read `port-destination.md`, `fork-trajectory-priority.md`, and `third-party-materials.md` when external material is involved, then route skills for the affected layers. Keep the history search path-scoped and report unavailable or conflicting evidence.

## Localization or player-visible text

Read `.agents/rules/content-and-localization.md` and route `localization` plus `localization-in-code` only when code resolves text; include the owning domain skill when relevant. Inspect the affected `en-US` owner file and directly referenced keys. By default, leave `ru-RU` untouched. If Russian localization is explicitly requested, update only its affected owner file and preserve variables, selectors, markup, and natural Russian wording.

## Cross-assembly or networked feature

Read the applicable scoped project guidance and `architecture-and-ownership.md`. Route client/server/shared, networking, prediction, actions, UI, or security skills according to the actual affected behavior. Verify declarations and project references before implementation; use the existing owner test project when behavior needs coverage.

## Prototype, map, asset, or structured resource

Read `yaml-prototype-conventions.md` for prototype formatting. Route the corresponding prototype, mapping, resource, localization, and YAML skills by the resources changed. Validate only the affected resource roots and references.

## Build, project, CI, or packaging change

Read `architecture-and-ownership.md` and route `module-architecture` and `build-and-packaging`. Confirm the current project graph and workflow before changing references or adding infrastructure.

## Broad review or feature port

For a user-requested broad review, read `code-review` plus rules and skills for changed surfaces. For a port, follow the history scenario above in addition to `port-destination.md`, `fork-trajectory-priority.md`, and `third-party-materials.md` when external material is involved; route the domain skills for the ported behavior. Do not turn a local task into a repository-wide audit.

## Unsupported access boundary

Verify the concrete declaration, access modifier, and assemblies. Do not use cross-assembly partial classes, reflection, or copied private logic. Stop and report the smallest supported extension point needed.
