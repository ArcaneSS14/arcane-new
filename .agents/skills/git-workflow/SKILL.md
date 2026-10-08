---
name: git-workflow
description: Preserve user work, scope, metadata, history, and verifiable delivery.
---

# Git Workflow

## Baseline

Start every write task with:

```text
git status --short --branch
git branch --show-current
git remote -v
```

Record existing modified and untracked paths, branch relationship, and any pre-existing SPDX diff. Preserve user-owned work.

## Scope control

Stage only task-related paths. Inspect the full staged diff before committing.

Agents MUST NOT add, edit, delete, normalize, or generate lines containing `SPDX-`. If a new file requires metadata, leave it for the user and report the path. Do not repair REUSE by changing SPDX.

Do not include generated files, logs, test results, IDE files, or unrelated cleanup unless requested.

## Shared history

Do not force-push, reset hard, clean untracked files, rewrite published history, or choose merge versus rebase without approval. Resolve conflicts by understanding both sides, not by selecting ours or theirs mechanically.

Every conflicted hunk has three versions: base, the incoming side, and the current branch. Identify all three before editing, then resolve by the priority order in `.agents/rules/merge-conflict-resolution.md`: an Arcane-marked change outranks both other versions, and otherwise the version being pushed forward wins unless the incoming side carries an independent reason to change.

## Commits

Follow the exact commit count and message requested by the user. If one commit is requested, do not create preparatory commits on the target branch.

A local task record is not a Git commit. A local commit is not a remote update. Verify the resulting SHA and branch ref before claiming delivery.

## Delivery gate

Inspect:

```text
git status --short --branch
git diff --name-status
git diff --check
git diff -U0
```

Confirm:

- only requested paths changed;
- no agent-introduced SPDX line changed;
- no user work was removed;
- no duplicate project or infrastructure appeared;
- all claimed checks actually ran;
- the target branch or PR head points to the reported commit.
