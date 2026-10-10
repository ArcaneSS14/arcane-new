---
name: tests-authoring
description: Author deterministic owner-correct tests that prove observable contracts through valid APIs.
---

A useful test fails for the original defect and proves a caller-visible contract.

Place focused root tests in `Content.Tests`, root integration tests in `Content.IntegrationTests`, and module integration tests in the existing `Modules/<Module>/Content.<Module>.IntegrationTests` project.

Do not create duplicate projects, fixtures, CI steps, MSBuild targets, or `module.yml` entries.

Act through the public API, real event, command, UI message, or lifecycle entry point used by production. Verify declarations, access modifiers, assemblies, runtime sides, and project references before using symbols.

Use controlled simulation time and deterministic randomness. Avoid wall-clock sleeps, machine locale, live services, order dependence, and leaked global state.

For localization behavior, test the requested locale contract. Require Russian parity, ordering, and culture-switch behavior only when Russian localization is in scope.

Run the focused test that covers the authored contract and build only its affected project when needed. Do not require a complete root or module test suite by default; expand to a full owner suite only when requested or when no narrower test can establish the result. Report exact filters and arguments.
