---
name: tests-authoring
description: Author deterministic owner-correct tests that prove observable contracts through valid APIs.
---

## Когда применять

Use when authoring or reviewing a focused regression test for a caller-visible contract.

## До изменения

Establish the owner, assembly, runtime side, access boundary, production entry point, original failing behavior, and expected observable outcome. Locate the existing owner test project and nearby test pattern before writing setup. Place focused root tests in `Content.Tests` and root integration tests in `Content.IntegrationTests`.

## Правила и границы

- A regression test must fail on the original defect and prove a caller-visible contract. Act through the production public API, real event, command, UI message, or lifecycle entry point.
- Verify declarations, access modifiers, assemblies, runtime sides, and project references before using a symbol. Do not use private reflection.
- Use controlled simulation time, deterministic randomness, and explicit setup/cleanup. Do not depend on wall-clock sleep, machine locale, live services, test order, or leaked global state.
- Cover boundary, permission, deletion, cancellation, reconnect, stale state, or provider-specific behavior when it belongs to the defect. Test Russian behavior only when Russian is in scope.
- Do not create duplicate projects, fixtures, CI steps, MSBuild targets, or `module.yml` entries.

### Source ledger

Claim: Focused root tests use `Content.Tests`, root integration tests use `Content.IntegrationTests`, and owner selection comes from current project/catalog evidence.
Source: `Modules/CATALOG.md`, current `.csproj` files, and neighboring owner tests.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing test project layout, catalog ownership, or test infrastructure.

### Minimal examples

Correct: make a deterministic test fail on the original caller-visible defect. Prohibited: reflect private state or sleep on wall clock. Stop: report inaccessible production entry point.

## Выбор подхода

Use a focused root test for a local contract and integration coverage only for a real cross-layer production path. Prefer the nearest existing owner fixture and deterministic control point over a broader asynchronous test harness.

## Антипаттерны

- Do not test inaccessible private state instead of observable behavior, mask the defect with setup shortcuts, or rely on timing/order coincidence.
- Do not claim regression coverage exists until the test is added and run.
- Do not expand to a full suite by default when the authored focused test and its affected build establish the contract.

## Проверка

Run the authored focused test and directly needed build when appropriate. Report the exact command/result and any check not run; never state that a test passed or coverage exists without executing it.

## References

- [Regression test design](references/regression-test-design.md): Read this when designing a regression test for a defect with boundary, permission, deletion, cancellation, reconnect, stale-state, or provider-specific risk.
