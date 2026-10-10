---
name: build-and-packaging
description: Change projects, solution structure, CI, generated content, and packaging with repository-correct ownership and exact commands.
---

Use this for project files, solution structure, MSBuild targets, workflows, module manifests, packaging rules, and generated outputs.

Verify repository identity, owner tag, owner-local module and underscore paths, assembly role, dependency direction, target framework, XAML imports, generated-code targets, solution grouping, and manifest role.

Do not introduce base-to-module references. Do not make Shared depend on Client or Server. Do not add integration-test projects to `module.yml`. Search for existing projects and CI steps before creating anything.

Inherited workflow or project changes require the current repository edit marker when the file format supports comments. Owner-local module and underscore paths do not receive redundant owner markers.

Copy commands and flags from the current repository. Do not import commands from another fork. Preserve submodule initialization, output paths, test arguments, and artifacts.

Configurations are `Debug`, `DebugOpt`, `Tools`, and `Release`. CI builds `DebugOpt`, tests the binaries it produced, and separately builds `Release` without the `/p:WarningsAsErrors=` override, so a `Release` build there treats warnings as errors.

Do not run `git submodule update` unless the user asked for it in the current request. It populates the engine checkout, and the engine is off limits by default per `.agents/rules/engine-boundaries.md`. If the submodule is missing, stop and report it instead of initializing it yourself.

For routine changes, follow root `AGENTS.md` and validate only the changed project or workflow. When the user requests CI-equivalent, release, or packaging validation, reproduce only the exact relevant workflow steps and report unavailable checks. Restore only when dependencies changed or are unavailable. Do not run `git submodule update` unless the user explicitly asks and `.agents/rules/engine-boundaries.md` permits it; if the engine submodule is missing, report it.

CI links `bin/Content.IntegrationTests/runtimes` into `bin/Content.Tests/` before running unit tests. Reproduce that only when a requested local test run fails on a missing native runtime rather than assuming the test itself is broken.

Run module integration projects, packaging platforms, and specialized validators only when they cover changed behavior or the requested CI/package target. Do not run every module suite or packaging platform by default.
