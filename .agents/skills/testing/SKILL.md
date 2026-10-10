---
name: testing
description: Select the existing owner test project and run checks matching the real failure mode.
---

Match tests to the owner and failure mode.

Use `Content.Tests` for focused root tests, `Content.IntegrationTests` for integrated root behavior, and the existing `Modules/<Module>/Content.<Module>.IntegrationTests` project for module behavior.

Search before creating a project, fixture, CI step, or MSBuild target. Duplicate infrastructure is forbidden. Test projects do not belong in `module.yml`.

Test through accessible public APIs or real event paths. Do not use reflection to reach private implementation.

For localization changes, verify the affected `en-US` keys and structure. Verify corresponding Russian contracts only when Russian localization is explicitly in scope.

Choose the smallest build and focused test that cover the changed behavior. Follow root `AGENTS.md`: do not restore by default or run full root and module suites automatically. Use the CI configuration when verifying a CI gate, and run broader suites only when requested or when the failure cannot be isolated and the risk justifies it. Report exact commands, failures, and omitted checks.

CI builds in `DebugOpt` and then tests the produced binaries, rather than passing a project path to `dotnet test`. Reproduce that shape only when verifying a CI gate. When running integration tests, use `DOTNET_gcServer=1`, matching CI. Use `--configuration Debug` only for fast local iteration, and say so, because `Debug` and `DebugOpt` differ in optimization and tool availability.
