---
name: debugging
description: Reproduce failures, trace authoritative flow, prove the broken assumption, and add targeted regression coverage.
---

# debugging

## Когда применять

Use when a runtime symptom, desync, UI issue, prototype-load error, localization defect, or regression needs a proven cause before changing design.

## До изменения

Reproduce the exact symptom and record inputs, runtime side, owner, lifecycle stage, and expected versus actual result. Establish the authoritative owner and public entry point first. Verify every assumed declaration, ID, resource, and path before altering the suspected flow.

## Правила и границы

- Trace only the affected flow: subscription, validation, mutation, dirtying, persistence, replication, and presentation. For a network defect, compare server and client state and identify the divergence point.
- Put temporary diagnostics only around the suspected divergence, remove them after the cause is confirmed, and never use them as a substitute for tracing ownership.
- Fix the narrow, proven broken assumption. Add regression coverage only through an existing owner test path and only when it can exercise the failure.
- For localization, inspect English root discovery, key, variables, selectors, loaded roots, and UI refresh. Inspect Russian only when the defect is Russian-specific or Russian is in scope.
- Apply repository markers only to inherited files; do not mark owner-local module or underscore paths.

### Minimal examples

Correct: reproduce and trace the authoritative divergence before fixing it. Prohibited: redesign from a symptom alone. Stop: report when the entry point or declaration cannot be proven.

## Выбор подхода

Choose a focused affected-project build after reproduction. Use a focused regression test when the owner can cover the failure; use integration coverage only for a broken cross-layer path. Treat a server/client comparison as required evidence for a network divergence rather than assuming which side is wrong.

## Антипаттерны

- Do not redesign before reproducing, fix a symptom without a proven owner, or leave temporary diagnostics in the final change.
- Do not infer a declaration, prototype ID, resource path, or replication step from a similar feature.
- Do not add a parallel test fixture or claim regression coverage that has not been added and run.

## Проверка

Record the reproduction result, the evidence for the divergence/cause, the narrow fix, and only commands actually run. Follow root `AGENTS.md`: do not restore dependencies or run an owner’s full suite by default.

## References

- [Debugging workflow](references/debugging-workflow.md): Read this when tracing a runtime failure, desync, UI issue, or prototype-load error.
