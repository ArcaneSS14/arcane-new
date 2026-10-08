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

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
dotnet test bin/Content.Tests/Content.Tests.dll -- NUnit.ConsoleOut=0 NUnit.TestOutputXml="logs" NUnit.WorkDirectory="$(pwd)/test_results"
$env:DOTNET_gcServer=1
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed NUnit.TestOutputXml="logs" NUnit.WorkDirectory="$(pwd)/test_results"
dotnet build --configuration Release --no-restore /m
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
dotnet build Content.Packaging --configuration Release --no-restore /m
```

CI links `bin/Content.IntegrationTests/runtimes` into `bin/Content.Tests/` before running unit tests. Reproduce that when a local unit-test run fails on a missing native runtime rather than assuming the test itself is broken.

Discover and run each affected module integration project. Run packaging platforms and specialized validators from exact current workflows. Report unavailable checks explicitly.
