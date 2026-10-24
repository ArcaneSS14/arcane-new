---
name: documentation
description: Write concise technical guidance, PR notes, architecture explanations, and reproducible verification reports.
---

## Когда применять

Use for technical guidance, PR notes, architecture explanations, handoffs, and reproducible verification reports.

## До изменения

Identify the canonical documentation owner, requested audience, scope, and decisions that are not obvious from the diff. For substantial work, determine only relevant owner, assembly boundary, data/event flow, compatibility, localization scope, resources, tests, and limitations.

## Правила и границы

- Document decisions and evidence, not a restatement of code. Use current paths, declarations, and exact commands; do not claim behavior from an old repository or source fork.
- A verification report separates commands actually run/results, checks not run/reason, manual checks, unresolved risks, and paths requiring human SPDX action.
- Do not state “all tests passed” when only a build or diff check ran. Do not turn internal implementation notes into a player changelog.
- Do not create a separate documentation file when an existing canonical owner already contains the information.
- Copy commands from current workflow/instructions; do not invent or simplify flags from memory.

## Выбор подхода

Write the smallest owner-local addition that explains a non-obvious decision, contract, limitation, or verification outcome. Omit fields that do not apply rather than filling a handoff with generic repository detail.

## Антипаттерны

- Do not invent paths, APIs, commands, behavior, test status, or source-fork assumptions.
- Do not report an unrun check as successful or hide an unavailable dependency behind a generic pass.
- Do not duplicate canonical documentation or create a changelog when technical guidance was requested.

## Проверка

Review every factual statement against the current file/declaration/command output. For documentation-only changes, use `git diff --name-status`, `git diff --check`, and final diff; state that builds/tests were not run unless they actually were.

## References

No reference is required for ordinary technical documentation; use the current workflow or owner guidance as the command source.
