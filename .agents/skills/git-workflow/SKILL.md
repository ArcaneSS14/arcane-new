---
name: git-workflow
description: Preserve user work, scope, metadata, history, and verifiable delivery.
---

# git-workflow

## Когда применять

Use for any write task that changes a Git worktree, staging, commit, conflict resolution, branch delivery, or remote publication.

## До изменения

Record `git status --short --branch`, the current branch, and remotes. Record existing staged, unstaged, and untracked paths, branch relationship, and any pre-existing SPDX diff. Preserve all user-owned work.

## Правила и границы

- Stage only task-related paths and inspect the full staged diff before committing. Do not include generated files, logs, test results, IDE files, or unrelated cleanup unless requested.
- Never add, edit, delete, normalize, or generate `SPDX-` lines. Report a file requiring human metadata instead of repairing REUSE.
- Do not force-push, `reset --hard`, clean untracked files, rewrite published history, or choose merge/rebase without approval.
- Resolve a conflict from base, incoming, and current versions, never mechanically with blanket `ours`/`theirs`. Follow `.agents/rules/merge-conflict-resolution.md`: choose the result that preserves confirmed behavior and intent; an incoming complete implementation may supersede an older Arcane divergence, while partial or unclear equivalence requires escalation.
- Create commits only with the exact count/message requested by the user. A local task record, local commit, and remote push are distinct states.

### Source ledger

Claim: Arcane conflict resolution is semantic: compare base, current, incoming, confirmed behavior, and intent; markers record provenance rather than an automatic winner.
Source: Root `AGENTS.md` and `.agents/rules/merge-conflict-resolution.md`.
Source type: Arcane
Checked against branch: `83d6c121f978aee06a4fbd70cf21ce968d0f8bc8`
Volatility: branch-bound
Recheck trigger: Changing repository conflict-resolution or edit-marker policy.

### Minimal examples

Correct: stage only reviewed task paths before a requested commit. Prohibited: reset hard to clean user work. Stop: report unresolved conflict intent instead of choosing ours/theirs.

## Выбор подхода

Keep work unstaged while investigating. Stage the smallest reviewed task slice only when a commit is requested. For a conflict, compare base, current, incoming, confirmed behavior, and intent before making the narrow semantic resolution; ask rather than selecting a side when equivalence is partial or intent is unclear.

## Антипаттерны

- Do not stage user work, create preparatory commits when one commit was requested, or claim a remote update from a local commit.
- Do not use destructive cleanup or history rewriting to obtain a clean status.
- Do not alter SPDX or silently drop untracked/user changes.

## Проверка

Before delivery inspect `git status --short --branch`, `git diff --name-status`, `git diff --check`, and final diff. Confirm only requested paths changed, user work remains, no agent SPDX change or duplicate infrastructure appeared, claimed checks ran, and the target remote ref points to the reported commit when publication was requested.

## References

No reference is required for ordinary Git delivery; read `.agents/rules/merge-conflict-resolution.md` when resolving a conflict.
