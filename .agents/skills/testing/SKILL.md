---
name: testing
description: Select the existing owner test project and run checks matching the real failure mode.
---

Match tests to the owner and failure mode.

Use `Content.Tests` for focused root tests, `Content.IntegrationTests` for integrated root behavior, and the existing `Modules/<Module>/Content.<Module>.IntegrationTests` project for module behavior.

Search before creating a project, fixture, CI step, or MSBuild target. Duplicate infrastructure is forbidden. Test projects do not belong in `module.yml`.

Test through accessible public APIs or real event paths. Do not use reflection to reach private implementation.

For localization changes, verify the affected `en-US` keys and structure. Verify corresponding Russian contracts only when Russian localization is explicitly in scope.

Run restore, the build in the configuration CI uses, the applicable root tests, and every affected module integration project. Report exact commands, failures, and omitted checks.

CI builds in `DebugOpt` and then tests the produced binaries, rather than passing a project path to `dotnet test`. Reproduce that shape locally when verifying behavior that CI gates. Run integration tests with `DOTNET_gcServer=1`, matching CI. Use `--configuration Debug` only when the goal is fast local iteration, and say so, because `Debug` and `DebugOpt` differ in optimization and tool availability.
