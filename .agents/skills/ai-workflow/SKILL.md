---
name: ai-workflow
description: Plan, verify, implement, and deliver repository work through evidence-backed ownership and quality gates.
---

# Plan and complete a scoped task

Establish the requested outcome, exclusions, allowed actions and relevant acceptance check. Inspect initial status/diff before writing. Resolve correctness-critical unknowns from the affected owner and declarations; ask only when a material decision cannot be established. Do not fill an all-layer preflight template for an unrelated surface.

Use [CATALOG](../../CATALOG.md) for changed surfaces and [SCENARIOS](../../SCENARIOS.md) for intersecting contracts. Track only relevant ownership, callers, lifecycle, resources and tests. Plan coherent behavior/owner blocks rather than an arbitrary file count. Complexity alone does not authorize delegation; if authorized, a delegated result still needs inspectable evidence and acceptance.

Reuse the existing implementation/extension point and preserve current work. Apply public policy from its owners rather than restating it here: [placement](../../rules/architecture-and-ownership.md), [markers](../../rules/arcane-edit-markers.md), [locales](../../rules/content-and-localization.md), [Git safety](../../rules/git-safety.md).

Choose evidence for the actual claim with [verification](../../rules/verification.md); inspect the final task diff and deliver under [handoff](../../rules/review-and-handoff.md). Track commit count and remote SHA only if that delivery was requested. A local fix does not authorize publication.
