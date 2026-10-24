---
name: code-review
description: Review correctness, access, repository ownership, authority, localization, compatibility, resources, scope, and verification evidence.
---

# Review criteria

Review the requested diff, behavior or finding. Establish the intended contract, then follow only affected producers/consumers and public policy. Identity/history checks are needed when ownership, provenance or synchronization depends on them, not for every comment.

A finding needs a reachable trigger → violated contract → concrete consequence, supported by a declaration, changed path, test/log or execution sequence. Give the narrow path/line and a useful correction. Treat speculation as an open hypothesis; style alone is not a defect without a credible correctness or maintenance impact.

Apply the relevant criteria conditionally:

- Access, layer direction and existing owners: [architecture](../../rules/architecture-and-ownership.md), [API](../../rules/coding-and-api-design.md).
- Inherited adaptations and resolved intent: [markers](../../rules/arcane-edit-markers.md), [merge policy](../../rules/merge-conflict-resolution.md).
- Client input/hidden state, transport/replay and lifecycle: security, networking, prediction or the changed domain skill.
- Serialized IDs/defaults, resource references and locale contracts: only their affected owners; [locale policy](../../rules/content-and-localization.md) does not require unsolicited Russian translation.
- Verification: a missing test file is not itself a defect. Identify an uncovered reachable regression and what existing evidence misses. Do not claim a command/CI gate ran without its actual result; follow [verification](../../rules/verification.md).
- Scope/metadata: preserve unrelated work and SPDX under [Git safety](../../rules/git-safety.md). Do not demand unrelated cleanup or duplicate infrastructure.

Severity follows impact, reachability and recoverability: critical for privilege compromise/widespread loss or repository-breaking behavior; high for exploitable authority errors/common crashes/severe desync/irreversible corruption; medium for reproducible wrong behavior or operational/compatibility failure; low for a localized maintenance issue with a concrete failure mode.

Review permission does not authorize fixing, staging, running implementation workflows or publishing. CodeRabbit applies these criteria to its available diff/evidence; executor procedures are not review obligations. Report material verification gaps under [handoff](../../rules/review-and-handoff.md).
