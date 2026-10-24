---
name: localization-in-code
description: Resolve localized text at presentation boundaries using English-source keys, with Russian localization only when requested, and correct lifecycle refresh.
---

# Resolve text at presentation boundaries

Keep logic, identity, equality, persistence and transport typed and language-independent. Do not store resolved output as durable state/cache or compare it as a machine identifier. Find the real key through [localization](../localization/SKILL.md); [locale policy](../../rules/content-and-localization.md) owns locale scope.

Pass typed entities, counts, durations and prototype-derived values. Match variable names/selector types and use existing entity-name/grammar helpers instead of manual pronoun/article concatenation. Refresh labels, tooltips, placeholders and generated rows when an already-supported culture change reaches a long-lived view; subscribe once and clean up. Do not create a new culture framework for every control.

For popup/feedback choose the recipient, prediction role, grammar and permitted information. Use the current predicted helper for local predicted feedback or authoritative helper for server-only outcomes; prevent duplication on confirmation. Reuse typed reasons where the same failure feeds UI state.

Inspect affected FTL plus the resolving code/XAML and choose checks under [verification](../../rules/verification.md). Culture-switch and constrained-text checks apply when the view/locale contract changes.
