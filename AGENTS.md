# Repository Agent Guidance

## Default scope

Work only within the smallest scope required by the current task.

Use paths, symbols, projects, modules, prototypes, locale keys, resources, and errors explicitly named by the user as the initial scope.

Do not inspect, inventory, summarize, or recursively enumerate the whole repository before starting work.

Do not read every `AGENTS.md`, `.agents/rules` file, skill, catalog, or scenario file.

Do not read every entry in `.agents/CATALOG.md` or `.agents/SCENARIOS.md`. Consult only the relevant catalog entry or matching scenario when task routing needs them; these files are indexes, not additional rules to load wholesale.

Do not inspect unrelated modules, forks, upstream repositories, projects, resources, localization trees, tests, or workflows.

## Instruction selection

Use instructions in this order:

1. System and developer instructions, then the user's current request.
2. This root `AGENTS.md` and the nearest `AGENTS.md` files applying to the paths being changed.
3. The applicable repository rules in `.agents/rules` and `CONTRIBUTING.md`.
4. Only the skills routed by `.agents/CATALOG.md` for the changed surfaces.

More specific instructions add detail to general instructions. If two applicable repository instructions still conflict, do not guess: report the conflict and use the instruction that best matches the user's requested scope, unless it would violate a higher-priority instruction.

Read the matching scenario when a task crosses layers, changes ownership or build structure, or needs a workflow beyond a routine owner-local change. For routine work, select the smallest set of directly applicable rules and skills. Cross-layer work can require several skills; do not omit a required one just to meet a fixed count.

When expanding the instruction scope, state which concrete changed surface requires it.

## Skill routing

Select skills from the actual requested change, not from hypothetical side effects. `.agents/CATALOG.md` is the canonical skill-routing index; consult only the entries relevant to changed surfaces, then read only those skills. A task spanning layers may require multiple skills.

This repository is based on TraumaStation and receives changes along its sync trajectory. `CONTRIBUTING.md` describes Arcane's project layout and contributor conventions. For changes intended for a Trauma-owned path, apply `.agents/rules/fork-trajectory-priority.md` and `.agents/rules/arcane-edit-markers.md`; do not use another fork's marker for our changes.

## Targeted discovery

Search for exact symbols, paths, prototype IDs, locale keys, resource paths, errors, or directly related types.

Search commands must use the narrowest practical directory or pathspec.

Preferred examples:

```sh
git grep -n "ExactSymbol" -- Content.Arcane.Server
git grep -n "exact-locale-key" -- Resources/Locale
git grep -n "PrototypeId" -- Resources/Prototypes
find Content.Arcane.Server/Feature -maxdepth 1 -type f
dotnet build Content.Arcane.Server/Content.Arcane.Server.csproj --no-restore
```

Do not begin with unrestricted commands such as:

```sh
find . -type f
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

Arcane runtime projects are the root-level `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client` projects. Arcane resources use the existing root `Resources` tree and `_Arcane` owner directories. Treat these as Arcane-owned unless current project metadata contradicts it. `Modules/Arcane` contains guidance files; it is not the runtime project root.

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
* over 5 changed lines: keep the active change inside an `Arcane-Edit` block; comment out removed content only when disabling it is the intended behavior
* merge adjacent Arcane blocks of the same kind into one pair, never merge an `Arcane-Start` block into an `Arcane-Edit-Start` block
* an added `using` goes after all others, inside an Arcane block or trailing `Arcane`
* never put a bare `-Start` or `-End` on a line instead of a pair

Never write `Trauma - `, `<Trauma>`, `Goobstation-`, `/* Trauma`, or any other fork's marker on our own change, whatever the surrounding file uses.

Arcane owner-local paths take no marker, because nothing syncs into them: `Content.Arcane.*`, `Resources/**/_Arcane/**`, and `Resources/Locale/**/_Arcane/**`.

Arcane changes inside `Content.Trauma.*`, `Resources/_Trauma/**`, `*.Trauma.cs`, `Content.Medical.*`, `Resources/_Shitmed/**`, and vanilla root paths require an Arcane marker when the file format supports comments. If a valid marker cannot be represented, stop before editing and report the limitation rather than corrupting the file or silently leaving an unmarked change.

Editing a Trauma file is allowed and often correct. Keep it cheap to reconcile:

1. append to the end of a list rather than inserting into sorted position
2. gather every addition in one file into a single block
3. prefer additive over destructive; achieve removals through a partial or `!Remove`
4. do not reformat, reorder, re-alphabetize, or tidy neighbouring lines
5. keep the hunk contiguous
6. record both sides when a value changes, as `Arcane-Edit: <old> > <new>`

Changing a line that carries an upstream marker makes that line ours, so the marker becomes `Arcane-Edit`. Leave upstream markers on lines we did not touch alone, and never convert them in bulk.

Prefer `Content.Arcane.*` and Arcane resource directories for Arcane-only behavior. Changes intended for the TraumaStation sync trajectory may belong in Trauma-owned or vanilla paths; keep those changes minimal, marked, and consistent with the inherited contribution rules.

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
