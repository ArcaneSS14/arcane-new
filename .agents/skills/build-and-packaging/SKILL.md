---
name: build-and-packaging
description: Change projects, solution structure, CI, generated content, and packaging with repository-correct ownership and exact commands.
---

# build-and-packaging

## Когда применять

Use for project files, solution structure, MSBuild targets, workflows, manifests, generated output, packaging rules, or CI-equivalent validation.

## До изменения

Determine owner, dependency direction, target framework, generated-code/XAML needs, output/artifact, actual CI workflow, solution grouping, and manifest role. Verify repository identity/owner tag/marker boundary when the change is inherited, changes ownership, project references, or automation.

## Правила и границы

- Do not create a duplicate project, CI step, module manifest entry, MSBuild target, or packaging path. Search existing projects/workflows first.
- Do not introduce a base-to-module reference or make Shared depend on Client/Server. Do not add integration-test projects to `module.yml`.
- Inherited workflow/project edits use the current repository marker when the format supports it; owner-local module/underscore paths do not receive redundant markers.
- Copy commands and flags from current repository workflows. Preserve actual output paths, artifacts, test arguments, and submodule assumptions; do not import commands from another fork.
- Repository configurations are `Debug`, `DebugOpt`, `Tools`, and `Release`. CI uses `DebugOpt`, tests produced binaries, and separately builds `Release` without `/p:WarningsAsErrors=` so that Release warnings are errors. `DOTNET_gcServer=1` belongs to the integration-test path. Reproduce these only for a requested CI-equivalent gate.
- Do not run `git submodule update` without explicit user request. The engine boundary is closed by default; if dependencies/submodule are unavailable, report the blocker rather than initializing them.

### Source ledger

Claim: CI uses `DebugOpt`, produced binaries, a separate `Release` build, and `DOTNET_gcServer=1` on integration tests.
Source: Current repository workflows and build configuration.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing workflow, build configuration, or integration-test invocation.

### Minimal examples

Correct: copy the exact current workflow command for a requested CI gate. Prohibited: initialize a missing submodule. Stop: report unavailable dependencies/workflow rather than inventing a build path.

## Выбор подхода

Validate only the changed project or workflow for routine work. Reproduce exact relevant CI/package steps only when the requested scope is CI-equivalent, release, or packaging. Run specialized validator/module integration/package platform only when it covers changed behavior; reproduce the `bin/Content.IntegrationTests/runtimes` link into `bin/Content.Tests/` only when a requested local test fails from a missing native runtime.

## Антипаттерны

- Do not invent build flags, targets, artifact paths, CI behavior, or project layout.
- Do not initialize a missing submodule, change engine content, or run every package/platform by default.
- Do not broaden a local project change into solution/CI/manifest work without an explicit need.

## Проверка

Inspect changed project/workflow files and actual owner command/source. Report unavailable dependencies/submodules and checks not run. Do not restore or run broad CI/package validation unless dependencies changed or the requested target requires it.

## References

- [Project change checklist](references/project-change-checklist.md): Read this when adding or changing a project, solution entry, build target, generated-code setting, or packaging input.
