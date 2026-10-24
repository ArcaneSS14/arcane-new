---
name: git-workflow
description: Preserve user work, scope, metadata, history, and verifiable delivery.
---

## Когда применять

Use for any write task that changes a Git worktree, staging, commit, conflict resolution, branch delivery, or remote publication.

## До изменения

Record `git status --short --branch`, the current branch, and remotes. Record existing staged, unstaged, and untracked paths, branch relationship, and any pre-existing SPDX diff. Preserve all user-owned work.

## Правила и границы

- Stage only task-related paths and inspect the full staged diff before committing. Do not include generated files, logs, test results, IDE files, or unrelated cleanup unless requested.
- Never add, edit, delete, normalize, or generate `SPDX-` lines. Report a file requiring human metadata instead of repairing REUSE.
- Do not force-push, `reset --hard`, clean untracked files, rewrite published history, or choose merge/rebase without approval.
- Resolve a conflict from base, incoming, and current versions, never mechanically with blanket `ours`/`theirs`. Follow `.agents/rules/merge-conflict-resolution.md`: an Arcane-marked change outranks both other versions; otherwise the pushed-forward version wins unless the incoming side independently requires change.
- Create commits only with the exact count/message requested by the user. A local task record, local commit, and remote push are distinct states.

## Выбор подхода

Keep work unstaged while investigating. Stage the smallest reviewed task slice only when a commit is requested. For a conflict, read all three versions and make the narrow semantic resolution; ask rather than selecting a side when the priority rule does not resolve intent.

## Антипаттерны

- Do not stage user work, create preparatory commits when one commit was requested, or claim a remote update from a local commit.
- Do not use destructive cleanup or history rewriting to obtain a clean status.
- Do not alter SPDX or silently drop untracked/user changes.

## Проверка

Before delivery inspect `git status --short --branch`, `git diff --name-status`, `git diff --check`, and final diff. Confirm only requested paths changed, user work remains, no agent SPDX change or duplicate infrastructure appeared, claimed checks ran, and the target remote ref points to the reported commit when publication was requested.

## References

No reference is required for ordinary Git delivery; read `.agents/rules/merge-conflict-resolution.md` when resolving a conflict.
