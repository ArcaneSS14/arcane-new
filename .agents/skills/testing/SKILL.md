---
name: testing
description: Select the existing owner test project and run checks matching the real failure mode.
---

# testing

## Когда применять

Use when selecting a build or test for a changed behavior, defect, lifecycle flow, networking/prediction path, resource change, or CI gate.

## До изменения

Determine the owner, failure mode, layer, and smallest observable contract. Select the existing owner project and nearby pattern through `Modules/CATALOG.md`, the relevant `.csproj`, and neighboring tests. Use `Content.Tests` for focused root tests and `Content.IntegrationTests` only for integrated root behavior.

## Правила и границы

- Test through a public API or real event, message, or lifecycle path; do not use reflection to reach private state.
- Do not create a project, fixture, CI step, MSBuild target, or `module.yml` test entry without a separate task. Reuse existing infrastructure.
- Choose the smallest build and focused test that covers the changed behavior. Do not restore or run full root/module suites automatically.
- Reproduce CI shape only when checking an actual CI gate. The build-and-test workflow builds `DebugOpt` then tests produced binaries, while HeisenDetector runs `dotnet test --no-build --configuration DebugOpt` against `Content.IntegrationTests.csproj`; select the named workflow's actual entry point. Integration-test invocations in both use `DOTNET_gcServer=1`. `Debug` is only fast local iteration and must be reported as different from `DebugOpt`.
- For localization, check affected `en-US` keys/structure; check Russian contracts only when Russian is explicitly in scope.

### Source ledger

Claim: CI test entry points differ by workflow: build-and-test uses produced binaries, and HeisenDetector uses `Content.IntegrationTests.csproj`; both integration invocations set `DOTNET_gcServer=1`.
Source: `build-test-debug.yml` and `heisendetector.yml`.
Source type: Arcane
Checked against branch: `83d6c121f978aee06a4fbd70cf21ce968d0f8bc8`
Volatility: branch-bound
Recheck trigger: Changing CI workflow, test runner, or build configuration.

### Minimal examples

Correct: select existing owner test project and focused observable contract. Prohibited: create a fixture/project for a local test. Stop: report unavailable owner coverage rather than inventing a command.

## Выбор подхода

Use an owner-local focused test for a local observable contract and integration coverage only when the behavior crosses the relevant root layers. Broaden only when the failure cannot be isolated or the task explicitly requires a CI/full-owner gate.

## Антипаттерны

- Do not invent a test command, project, CI configuration, fixture, or build target.
- Do not use private reflection, automatic restore, or a full suite as the first response to a local failure.
- Do not claim CI equivalence after a Debug-only local run.

## Проверка

Report commands actually run, their exact result, and omitted checks with their reason. Select the smallest meaningful verification; skill-only documentation changes require `git diff --check`, `git diff --stat`, and final diff rather than builds/tests. Validate affected links and paths when the change alters routing, links, or paths.

## References

- [Testing matrix](references/testing-matrix.md): Read this when selecting verification for lifecycle, networking, prediction, resources, UI, migrations, packaging, or documentation changes.
