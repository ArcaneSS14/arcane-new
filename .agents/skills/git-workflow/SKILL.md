---
name: git-workflow
description: Preserve user work, scope, metadata, history, and verifiable delivery.
---

# Scoped Git work

For a write task inspect status and the affected diff, including pre-existing tracked/untracked work. Inspect branch/remotes/history only when the requested operation or ownership depends on them. Follow [Git safety and metadata](../../rules/git-safety.md) for destructive operations, SPDX and user work.

Stage only reviewed task paths and inspect the full staged diff before committing. Keep logs, generated outputs and unrelated cleanup out unless requested. Follow the user's commit count/message/scope; logical layers in a port inventory do not require separate commits.

An actual conflict follows [semantic merge policy](../../rules/merge-conflict-resolution.md), not a marker-based side-selection shortcut. Choosing a history strategy is separate from inspecting history.

Inspect final status/diff and git diff --check. A local record, local commit, pushed ref and PR are distinct outcomes: verify only the requested delivery's resulting SHA/ref before claiming it. Commit or push authorization does not authorize merge; only the user merges.
