# Repository Agent Guidance

## Default scope

Work only within the smallest scope required by the current task.

Use paths, symbols, projects, modules, prototypes, locale keys, resources, and errors explicitly named by the user as the initial scope.

Do not inspect, inventory, summarize, or recursively enumerate the whole repository before starting work.

Do not read every `AGENTS.md`, `.agents/rules` file, skill, catalog, or scenario file.

Do not read `.agents/CATALOG.md` or `.agents/SCENARIOS.md` unless the user explicitly requests:

* a repository-wide review
* a full pull request review
* a complete feature port
* a broad architecture change
* a final pre-merge audit

Do not inspect unrelated modules, forks, upstream repositories, projects, resources, localization trees, tests, or workflows.

## Instruction selection

For a normal task, initially read only:

1. this root `AGENTS.md`
2. the nearest scoped `AGENTS.md` for each explicitly targeted path
3. no more than two directly relevant skills from `.agents/skills`

A third skill may be read only when the current code proves that another technical surface is directly affected.

Do not open a skill merely because it could be generally useful.

Do not read both a broad skill and all of its neighboring domain skills by default.

When expanding instruction scope, state the concrete changed surface that requires the additional instruction.

## Skill routing

Select skills from the actual requested change, not from hypothetical side effects.

This table routes every skill in `.agents/skills`. If a task matches a row, that skill is the required read. Always-on constraints live in `.agents/rules` and are not repeated here.

### Ownership, process, and delivery

* End-to-end feature across all layers: `.agents/skills/gameplay-feature/SKILL.md`
* Plan, preflight, and delivery quality gates: `.agents/skills/ai-workflow/SKILL.md`
* Cross-project ownership or project references: `.agents/skills/module-architecture/SKILL.md`
* Staging, commits, history, and scope control: `.agents/skills/git-workflow/SKILL.md`
* Resolving merge conflicts or choosing a side: `.agents/rules/merge-conflict-resolution.md`
* Marking Arcane changes to inherited files: `.agents/rules/arcane-edit-markers.md`
* Choosing where a change belongs, or handling Trauma and vanilla upstream files: `.agents/rules/fork-trajectory-priority.md`
* Porting a complete feature family: `.agents/rules/port-destination.md`

`arcane-new` is a fork of TraumaStation, and TraumaStation is the sync source. `CONTRIBUTING.md` is the inherited TraumaStation contribution guide and is authoritative for Trauma-owned code: all new C# in `Content.Trauma.*`, `.Trauma.cs` partials for additions to base files, no new handlers on upstream systems, resources under `_Trauma`, partial prototypes in `Resources/Prototypes/_Trauma/Partials`, and `Trauma - reason` or `<Trauma>` markers on upstream edits. Read it before changing anything on the Trauma trajectory.
* Inherited upstream code and edit markers: `.agents/skills/upstream-maintenance/SKILL.md`
* Porting a complete feature family: `.agents/skills/porting/SKILL.md`
* Reviewing a change or pull request: `.agents/skills/code-review/SKILL.md`
* Reproducing and proving a failure: `.agents/skills/debugging/SKILL.md`
* Technical docs, PR notes, and reports: `.agents/skills/documentation/SKILL.md`
* Naming symbols, events, prototypes, keys, and resources: `.agents/skills/naming-conventions/SKILL.md`

### Code, assemblies, and performance

* Local C# implementation and symbol access: `.agents/skills/csharp-style/SKILL.md`
* Hot paths, allocations, iteration, and caching: `.agents/skills/performance/SKILL.md`
* Logging, diagnostics, and player-facing failures: `.agents/skills/logging-and-errors/SKILL.md`
* Trust boundaries, abuse resistance, and secrets: `.agents/skills/security-and-validation/SKILL.md`

### ECS

* Reading an unfamiliar subsystem first: `.agents/skills/ecs-basics/SKILL.md`
* Serialized component state: `.agents/skills/ecs-components/SKILL.md`
* Event choice, direction, and cancellation: `.agents/skills/ecs-events/SKILL.md`
* System behavior and subscriptions: `.agents/skills/ecs-systems/SKILL.md`
* EntitySystem helpers and prototype APIs: `.agents/skills/entity-api-patterns/SKILL.md`
* DataFields and serialization contracts: `.agents/skills/serialization-and-datafields/SKILL.md`

### Client, server, and shared

* Client, server, or shared boundaries: `.agents/skills/client-server-shared/SKILL.md`
* Network state or network events: `.agents/skills/networking/SKILL.md`
* Predicted execution: `.agents/skills/prediction/SKILL.md`
* Visibility, network interest, and PVS: `.agents/skills/pvs/SKILL.md`

### Gameplay behavior

* Verbs, in-hand use, and reusable interactions: `.agents/skills/interaction-flow/SKILL.md`
* Actions, cooldowns, and DoAfter flows: `.agents/skills/actions-and-doafter/SKILL.md`
* Spawning, initialization, transfer, and deletion: `.agents/skills/entity-lifecycle-and-spawning/SKILL.md`
* Entity references, links, and target relations: `.agents/skills/entity-relations-and-links/SKILL.md`
* Containers, hands, slots, and inventories: `.agents/skills/containers-and-inventory/SKILL.md`
* Grids, coordinates, anchoring, and physics: `.agents/skills/transform-and-physics/SKILL.md`
* Timers, cancellation, and async work: `.agents/skills/timers-and-async/SKILL.md`
* Damage, healing, status effects, and modifiers: `.agents/skills/damage-status-and-effects/SKILL.md`
* Round lifecycle, rules, and win conditions: `.agents/skills/round-and-game-rules/SKILL.md`
* Minds, sessions, bodies, roles, and objectives: `.agents/skills/minds-roles-and-objectives/SKILL.md`
* NPC behavior, HTN tasks, and navigation: `.agents/skills/npc-ai/SKILL.md`
* Commands, CVars, and CVar-backed configuration: `.agents/skills/commands-and-cvars/SKILL.md`
* Privileged commands and admin controls: `.agents/skills/admin-and-permissions/SKILL.md`
* Persistent models and migrations: `.agents/skills/database-migrations/SKILL.md`
* File and configuration persistence: `.agents/skills/save-data-and-configuration/SKILL.md`
* Authoritative and weighted randomness: `.agents/skills/randomness-and-determinism/SKILL.md`

### Content and gameplay systems

* Construction graphs and machine lifecycle: `.agents/skills/construction-and-machines/SKILL.md`
* Reagents, reactions, and metabolism: `.agents/skills/chemistry-and-reagents/SKILL.md`
* Atmospherics, gases, fire, and pressure: `.agents/skills/atmos/SKILL.md`
* Weighted collections and datasets: `.agents/skills/collections-and-datasets/SKILL.md`

### Presentation

* XAML controls: `.agents/skills/xaml-ui/SKILL.md`
* Bound user interfaces: `.agents/skills/bound-user-interface/SKILL.md`
* EUI sessions and state: `.agents/skills/eui/SKILL.md`
* Input validation and feedback: `.agents/skills/forms-and-input-validation/SKILL.md`
* Appearance data and visualizer mappings: `.agents/skills/appearance-and-visualizers/SKILL.md`
* Sprite layers, overlays, and shaders: `.agents/skills/sprite-overlays-and-shaders/SKILL.md`
* Data-driven audio: `.agents/skills/audio/SKILL.md`

### Content and resources

* FTL localization: `.agents/skills/localization/SKILL.md`
* Localized values used from code: `.agents/skills/localization-in-code/SKILL.md`
* YAML prototypes: `.agents/skills/prototypes/SKILL.md`
* Prototype display text: `.agents/skills/prototype-localization/SKILL.md`
* General YAML or schema work: `.agents/skills/yaml-and-schema/SKILL.md`
* Assets or resource paths: `.agents/skills/resources-and-assets/SKILL.md`
* Maps and map prototypes: `.agents/skills/maps-and-mapping/SKILL.md`

### Integration and verification

* External HTTP, webhooks, and processes: `.agents/skills/external-services/SKILL.md`
* Build, solution, CI, or packaging changes: `.agents/skills/build-and-packaging/SKILL.md`
* Creating or changing tests: `.agents/skills/tests-authoring/SKILL.md`
* Selecting verification commands: `.agents/skills/testing/SKILL.md`

Examples:

* A local C# fix normally requires only `csharp-style`.
* Writing or reviewing C# layout, reuse, or idiom choice: `.agents/rules/csharp-writing-conventions.md`
* Adding or removing comments, or judging whether a comment belongs: `.agents/rules/commenting-conventions.md`
* Writing or reviewing systems, components, events, or prototype types: `.agents/rules/ecs-writing-conventions.md`
* Writing or reviewing prototype and resource YAML: `.agents/rules/yaml-prototype-conventions.md`
* An FTL wording correction normally requires only `localization`.
* A prototype with a new visible name normally requires `prototypes` and `prototype-localization`.
* A networked component normally requires `client-server-shared` and `networking`.
* A XAML layout correction normally requires only `xaml-ui`.
* A test-only correction normally requires `tests-authoring` and `testing`.

## Targeted discovery

Search for exact symbols, paths, prototype IDs, locale keys, resource paths, errors, or directly related types.

Search commands must use the narrowest practical directory or pathspec.

Preferred examples:

```powershell
git grep -n "ExactSymbol" -- Modules/Arcane/Content.Arcane.Server
git grep -n "exact-locale-key" -- Modules/Arcane/Resources/Locale
git grep -n "PrototypeId" -- Modules/Arcane/Resources/Prototypes
Get-ChildItem Modules/Arcane/Content.Arcane.Server/Feature -File
dotnet build Modules/Arcane/Content.Arcane.Server/Content.Arcane.Server.csproj --no-restore
```

Do not begin with unrestricted commands such as:

```powershell
Get-ChildItem -Recurse
git grep -n "generic-term"
rg "generic-term" .
dotnet build SpaceStation14.slnx
dotnet test
```

An unrestricted repository-wide search is allowed only when:

* the user explicitly requests repository-wide analysis
* the exact owner cannot be found through targeted searches
* a public compatibility surface is being renamed
* the task explicitly requires finding every reference

Stop expanding once enough evidence exists to implement the requested change.

## Repository identity

Do not perform a full repository identity audit for every task.

For work inside a clearly owner-local path such as `Modules/Arcane`, treat that path as Arcane-owned unless nearby project or module metadata contradicts it.

Run repository, remote, upstream, owner-tag, and edit-marker discovery only when the task changes:

* an inherited file outside owner-local paths
* module or project ownership
* project references
* upstream synchronization
* edit markers
* repository automation

When required, use targeted identity checks:

```powershell
git rev-parse --show-toplevel
git branch --show-current
git remote -v
git log -1 --oneline
```

Do not scan every source file for edit markers unless an inherited file is actually being changed.

## Evidence before changes

Do not invent APIs, symbols, events, types, paths, prototype IDs, locale keys, resources, projects, or framework behavior.

Verify declarations only for symbols that the implementation will actually call or modify.

For a non-local symbol, verify the declaration, accessibility, namespace, owning project, caller project, and required project reference.

Do not inspect unrelated assemblies or dependency graphs after the required access has already been proven.

If required state is inaccessible and no supported extension point exists, stop and report the concrete declaration and access boundary. Do not use reflection or copy private implementation logic.

## Engine access

Do not modify anything inside `RobustToolbox/`, run commands inside the engine, move the submodule pointer, or treat engine internals as an implementation option unless the user explicitly asked for an engine change in their current request.

When content cannot reach something the engine owns, implement the smallest legitimate content-side extension point, or STOP and report the declaration, its access modifier, the involved assemblies, and the engine boundary that would need crossing. Then wait for the user. Propose the engine change; do not begin it.

Reading the engine to verify a signature is allowed and is not an engine change. Read-only validators that CI runs against content are allowed as well. Full rules: `.agents/rules/engine-boundaries.md`.

## Ownership and edit markers

This section is always in context for Codex and OpenCode. The canonical text is `.agents/rules/arcane-edit-markers.md`, `.agents/rules/fork-trajectory-priority.md`, and `.agents/rules/merge-conflict-resolution.md`.

Arcane is a fork of TraumaStation and TraumaStation is the sync source, so inherited and Trauma-owned files are the conflict surface.

Our own changes carry Arcane markers only:

* one added line: trailing `// Arcane` or `# Arcane`, no `-Start` / `-End`
* two or more added lines: `// Arcane-Start` / `// Arcane-End`, `# Arcane-Start` / `# Arcane-End`
* one changed line: trailing `// Arcane-Edit: <old> > <new>` or `# Arcane-Edit: <old> > <new>`
* two or more changed lines: `// Arcane-Edit-Start` / `// Arcane-Edit-End`, `# Arcane-Edit-Start` / `# Arcane-Edit-End`
* over 5 changed lines: comment the payload inside the `Arcane-Edit-Start` / `Arcane-Edit-End` block
* merge adjacent Arcane blocks of the same kind into one pair, never merge an `Arcane-Start` block into an `Arcane-Edit-Start` block
* an added `using` goes after all others, inside an Arcane block or trailing `Arcane`
* never put a bare `-Start` or `-End` on a line instead of a pair

Never write `Trauma - `, `<Trauma>`, `Goobstation-`, `/* Trauma`, or any other fork's marker on our own change, whatever the surrounding file uses.

Arcane owner-local paths take no marker, because nothing syncs into them: `Modules/Arcane/**`, `Content.Arcane.*`, `Resources/_Arcane/**`.

Arcane changes inside `Content.Trauma.*`, `Resources/_Trauma/**`, `*.Trauma.cs`, `Content.Medical.*`, `Resources/_Shitmed/**`, and vanilla root paths require an Arcane marker. Those paths are owner-local for us but upstream surface for a sync, so an unmarked change there is indistinguishable from a Trauma change and gets reverted.

Editing a Trauma file is allowed and often correct. Keep it cheap to reconcile:

1. append to the end of a list rather than inserting into sorted position
2. gather every addition in one file into a single block
3. prefer additive over destructive; achieve removals through a partial or `!Remove`
4. do not reformat, reorder, re-alphabetize, or tidy neighbouring lines
5. keep the hunk contiguous
6. record both sides when a value changes, as `Arcane-Edit: <old> > <new>`

Changing a line that carries an upstream marker makes that line ours, so the marker becomes `Arcane-Edit`. Leave upstream markers on lines we did not touch alone, and never convert them in bulk.

Prefer `Modules/Arcane` for Arcane-only behavior and `Content.Trauma.*` for upstream-compatible behavior. For a feature port, `Content.Trauma.*` survives a sync while `Modules/Arcane` does not.

When a sync conflict must be resolved, read all three versions and merge semantically. The resolved line takes the marker of whoever wrote it, and our resolution is ours, so it carries an Arcane marker. Never blanket-resolve with `git checkout --ours/--theirs`, `-X ours`, or `-X theirs`, and never `git add -u` a path you did not read.

Never add, edit, remove, reorder, normalize, copy, or generate a line containing `SPDX-` in game code, resources, or configuration unless the user's current request provides the exact SPDX change. Agent instruction files under `.agents/`, `.claude/`, `.cursor/`, and `.codex/` carry no SPDX headers.

Determine marker requirements only for files that will actually be modified.

Do not scan or classify unrelated files.

## Localization

For changed localization entries, `en-US` is the structural source of truth.

When an English message is added, removed, renamed, moved, reordered, or structurally changed, apply the matching change to the corresponding `ru-RU` file.

Russian localization must use natural wording and must not contain `THE(...)` or equivalent English grammar wrappers.

Compare only the affected locale files and directly referenced keys. Do not enumerate the complete locale tree for a local correction.

## Implementation

Inspect the current implementation and nearby files before introducing a new abstraction.

Prefer the existing owner, system, component, prototype file, resource directory, locale file, test project, and extension point.

Do not create parallel managers, helpers, projects, fixtures, CI steps, resource hierarchies, or locale files when an existing owner already covers the requested behavior.

Validate client-originated requests on the server when the task crosses a trust boundary.

Keep compatibility-sensitive identifiers stable unless the user explicitly requests their migration.

## Verification

Use the smallest verification set covering the files actually changed.

Do not automatically run:

* `git submodule update --init --recursive`
* `dotnet restore`
* a full solution build
* all unit tests
* all integration tests
* both Debug and Release builds
* every linter
* every packaging check

For a local C# change, build the directly affected project.

For a test change, run the directly affected test project or filtered test.

For localization or prototype changes, run only the relevant validation when such a targeted command exists.

For documentation or agent-instruction changes, use diff checks only unless executable behavior changed.

Always run:

```powershell
git diff --check
git diff --stat
```

Inspect the final diff for the files changed by the task.

Run full repository verification only when the user explicitly requests full validation, final PR validation, release validation, or a repository-wide audit.

Do not include large successful build or test logs in model context. Preserve the command result and inspect only relevant warnings or failures.

When a command fails, narrow the output to the first actionable errors before further analysis.

## Delivery

Before reporting completion, verify only the surfaces touched by the task:

* changed files belong to the requested scope
* used symbols exist and are accessible
* required localization counterparts were updated
* no unrelated files were modified
* claimed verification commands actually ran
* the final diff matches the requested outcome

Do not claim checks that were not run.

Do not rewrite published history, discard user changes, remove untracked files, or perform destructive cleanup without explicit approval.
