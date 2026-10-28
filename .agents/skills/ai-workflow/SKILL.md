---
name: ai-workflow
description: Plan, verify, implement, and deliver repository work through evidence-backed ownership and quality gates.
---

# ai-workflow

## Когда применять

Use for repository work that needs an evidence-backed plan, implementation boundary, verification choice, or delivery report.

## До изменения

Establish the requested outcome, exclusions, allowed paths, commit/push expectation, and verification plan. Collect only relevant preflight: owner and owner-local paths, inherited paths/marker syntax, caller/target assemblies, required declarations/access, extension point, test owner, locale scope, and verification. Mark irrelevant fields not applicable; do not begin implementation while a correctness-critical field remains unknown.

## Правила и границы

- Treat declarations, files, and command output as evidence. Subagent summaries and unverified assumptions are not proof.
- Verify repository/remote/upstream, branch, and marker scheme only when inherited paths, ownership, project references, synchronization, or automation are in scope.
- Reuse the existing owner path and infrastructure. Mark inherited changes with the current repository marker; Arcane-owned paths need no ownership marker. Follow the [root SPDX boundary](../../../AGENTS.md#ownership-and-edit-markers), including its exact-request exception; do not perform incidental metadata cleanup.
- English localization is structurally canonical; update Russian only when explicitly requested.
- Run the narrowest meaningful check first. A missing dependency or tool is a limitation to report, never a pass.

### Minimal examples

Correct: use declarations and command output as evidence for the changed owner. Prohibited: implement from an unverified summary. Stop: report a correctness-critical unknown boundary before implementation.

## Выбор подхода

Map entry points, dependencies, lifecycle, resources, localization, and tests using exact paths/symbols only for the changed surface. Choose the smallest existing extension point and owner-local implementation that satisfies the outcome; broaden exploration or verification only when evidence requires it.

## Антипаттерны

- Do not implement around an unknown access/owner/authority boundary, invent APIs, or treat a summary as declaration evidence.
- Do not expand to unrelated files, infrastructure, localization, or full verification merely to complete a checklist.
- Do not inspect or change SPDX when code/resources/configuration are not in scope.

## Проверка

Inspect final status and diff. In delivery, separate checks actually run and their results, checks not run and why, limitations/unresolved risks, and remote status. Verify commit count, resulting SHA, remote ref, or PR head only when the task includes commit, push, or PR publication.

## References

- [Evidence and verification](references/evidence-and-verification.md): Read this when weighing conflicting evidence, reporting a failed check, or handing off an uncertainty.
- [Prompt patterns](references/prompt-patterns.md): Read this when writing an implementation or review prompt for an agent.
- [Task decomposition](references/task-decomposition.md): Read this when dividing a cross-layer task into parallel workstreams.
