---
name: testing
description: Select the existing owner test project and run checks matching the real failure mode.
---

## Когда применять

Use when selecting a build or test for a changed behavior, defect, lifecycle flow, networking/prediction path, resource change, or CI gate.

## До изменения

Determine the owner, failure mode, layer, and smallest observable contract. Select the existing owner project and nearby pattern through `Modules/CATALOG.md`, the relevant `.csproj`, and neighboring tests. Use `Content.Tests` for focused root tests and `Content.IntegrationTests` only for integrated root behavior.

## Правила и границы

- Test through a public API or real event, message, or lifecycle path; do not use reflection to reach private state.
- Do not create a project, fixture, CI step, MSBuild target, or `module.yml` test entry without a separate task. Reuse existing infrastructure.
- Choose the smallest build and focused test that covers the changed behavior. Do not restore or run full root/module suites automatically.
- Reproduce CI shape only when checking an actual CI gate: CI builds in `DebugOpt` then tests produced binaries, rather than passing a project path to `dotnet test`; integration tests use `DOTNET_gcServer=1`. `Debug` is only fast local iteration and must be reported as different from `DebugOpt`.
- For localization, check affected `en-US` keys/structure; check Russian contracts only when Russian is explicitly in scope.

## Выбор подхода

Use an owner-local focused test for a local observable contract and integration coverage only when the behavior crosses the relevant root layers. Broaden only when the failure cannot be isolated or the task explicitly requires a CI/full-owner gate.

## Антипаттерны

- Do not invent a test command, project, CI configuration, fixture, or build target.
- Do not use private reflection, automatic restore, or a full suite as the first response to a local failure.
- Do not claim CI equivalence after a Debug-only local run.

## Проверка

Report commands actually run, their exact result, and omitted checks with their reason. Select the smallest meaningful verification; skill-only documentation changes require diff checks rather than builds/tests.

## References

- [Testing matrix](references/testing-matrix.md): Read this when selecting verification for lifecycle, networking, prediction, resources, UI, migrations, packaging, or documentation changes.
