---
name: ai-workflow
description: Plan, verify, implement, and deliver repository work through evidence-backed ownership and quality gates.
---

# AI Workflow

## Establish the contract

Resolve the requested outcome, exclusions, allowed paths, and required checks. Verify repository owner, upstream, branch, and edit-marker scheme when the work changes inherited files, ownership, project references, synchronization, or automation. Track commit count and remote branch SHA only when the task includes committing, pushing, or publishing a PR.

Record the initial worktree state before editing. Inspect SPDX changes only when game code, resources, or configuration are in scope.

## Complete the preflight

Fill in only fields relevant to the current task. Mark the others not applicable instead of investigating unrelated surfaces.

```text
Repository and owner tag:
Owner module and underscore paths:
Inherited paths and marker syntax:
Target and caller assemblies:
Required declarations and access:
Existing extension point:
Existing test owner:
Localization owner and mirrored files:
Verification plan:
```

No implementation starts while a correctness-critical field for the current task is unknown. Skip fields that do not apply rather than performing unrelated discovery.

## Explore before designing

Map entry points, dependencies, resources, tests, lifecycle, localization order, and current behavior using exact paths and symbols.

Subagent summaries are not proof without declarations, file evidence, or command output.

## Implement narrowly

Reuse existing owners and infrastructure. Mark inherited edits with the current repository marker. Do not mark owner-local module or underscore paths. Do not edit SPDX.

Treat English localization as the structural source of truth and mirror every structural change in Russian at the same relative position.

## Verify claims

Run the narrowest meaningful check first, then broaden according to risk. A missing tool is a limitation, not a pass.

## Deliver

Inspect final status and diff. Confirm the applicable scope, marker placement, accessibility, localization parity, duplicate infrastructure, and exact verification results. Confirm commit count and remote branch SHA only if commits or remote publication were part of the task.
