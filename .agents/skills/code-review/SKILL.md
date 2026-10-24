---
name: code-review
description: Review correctness, access, repository ownership, authority, localization, compatibility, resources, scope, and verification evidence.
---

## Когда применять

Use when reviewing a change for observable correctness, architectural validity, scope, evidence, and actionable defects; review formatting and style last.

## До изменения

Determine intended scope and observable behavior, owner, authority, assembly/access boundary, lifecycle, persistence, networking/prediction, resources, and localization impact. For inherited, ownership, project-reference, or synchronization changes, verify repository identity, owner tag, and upstream. For changed inherited paths, identify the applicable edit-marker rule; owner-local module and underscore paths must not gain redundant markers.

## Правила и границы

- Trace validation, mutation, dirtying, persistence, lifecycle, presentation, repeated execution, reconnect, and failure paths only for changed surfaces.
- Check declarations, access modifiers, project/assembly references, public contracts, cleanup, compatibility, resources, and regression behavior relevant to the diff.
- Verify the English FTL contract: keys, attributes, variables, selectors, paths, and order. Review Russian coverage/parity only when requested or when the issue is Russian-specific.
- Inspect the complete diff for unrelated paths, SPDX edits, duplicated infrastructure, invented APIs/commands, and unproven verification claims.
- Every finding needs path, concrete declaration/execution evidence, impact, severity, and minimal remediation. A stylistic preference without observable impact is not a defect.

### Minimal examples

Correct: report an evidence-backed observable defect with path and impact. Prohibited: call a style preference a bug. Stop: report insufficient declaration/diff evidence instead of asserting a finding.

## Выбор подхода

Start with correctness/authority/architecture, then lifecycle and safety, then test evidence and scope, then maintainability/style. Report only evidence-backed findings; distinguish a confirmed defect from an unverified risk or a request for clarification.

## Антипаттерны

- Do not review formatting before observable behavior, invent an API/command, or call a preference a bug without impact.
- Do not require markers on owner-local paths or skip marker review for an actually changed inherited path.
- Do not claim a test/build result from an unrun command or ignore an unrelated/unauthorized diff path.

## Проверка

Inspect the complete changed-file list and final diff. Confirm each finding's evidence and scope, and state explicitly which verification evidence was unavailable rather than inferring success.

## References

- [Severity and evidence](references/severity-and-evidence.md): Read this when ranking findings or deciding whether observed evidence supports a defect report.
