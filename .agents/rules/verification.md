# Verification

Start with the smallest sufficient check for the changed contract. Broaden when a concrete uncovered risk or project gate justifies it, up to the full set if necessary. Explain the reason for expansion and respect the current task's execution restrictions. Selecting GitHub checks does not authorize duplicating a full run locally.

Do not automatically restore dependencies, build the solution, run both configurations, all tests or packaging. Restore only for changed dependencies, missing assets or a workflow that requires it. Reuse results only for matching code, dependencies, configuration and artifacts; wait for the producing build before a dependent --no-build run.

## Current owners and command sources

| Changed surface | Smallest meaningful starting point |
| --- | --- |
| C# | Build the affected existing project, e.g. dotnet build Content.Arcane.Shared/Content.Arcane.Shared.csproj --no-restore when restore assets exist |
| Test | Existing Content.Tests or Content.IntegrationTests owner; filter to the changed scenario where supported. Do not invent Modules/<Module>/*.IntegrationTests.csproj |
| Integrated client/server behavior | Content.IntegrationTests; set DOTNET_gcServer=1 for its run and verify selected/executed count |
| Prototypes / locale / other structured data | Existing validator for the actual loader and affected resources; syntax parsing alone does not establish domain validity |
| RSI | CI's read-only RobustToolbox/Schemas/validate_rsis.py validator; inspect supported arguments before narrowing its resource input |
| Packaging | Affected Content.Packaging path/target and its resulting artifact |
| Documentation / agent instructions | Final diff, affected links, anchors and routes; separately exercise a changed executable helper |

Runtime projects are at the root. Find actual project ownership through [Modules/CATALOG.md](../../Modules/CATALOG.md); do not infer test projects from a module naming template.

SDK/TFM/language come from global.json and MSBuild/Content.props plus pinned engine props. Inspect those files when the version matters. Debug, DebugOpt, Tools and Release have different assertions, tools and optimization behavior; choose the configuration exercising the relevant contract.

## CI reproduction

Use the affected workflow's exact command and artifact identity. .github/workflows/build-test-debug.yml tests built DLLs; .github/workflows/heisendetector.yml tests a csproj with --no-build. Do not describe every gate as the same command. YAML linting builds Release before its --no-build linter run. Reproduce only the needed gate unless broader validation is justified.

MSBuild/Content.props enables TreatWarningsAsErrors for Release. WarningsAsErrors is a separate diagnostic list: passing /p:WarningsAsErrors= does not generally turn off TreatWarningsAsErrors. Do not hide unexplained warnings by changing either property. Record the effective command/configuration and distinguish compiler warnings from other build diagnostics.

[Engine boundaries](engine-boundaries.md) allow addressed source reads and CI's read-only content validator, not submodule edits or commands inside it.

Inspect the final diff and run git diff --check and git diff --stat. Record actual exit/results and material omissions. Zero selected tests, stale artifacts or unavailable tools are not successful verification. Code inspection does not prove runtime or visual behavior. For check selection see [testing](../skills/testing/SKILL.md).
