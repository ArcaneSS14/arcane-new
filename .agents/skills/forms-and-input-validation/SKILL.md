---
name: forms-and-input-validation
description: Validate UI, command, text, numeric, entity, and configuration input with localized feedback and server authority.
---

# Input syntax and domain feedback

Define accepted syntax and normalized representation. Trim/case-fold only when the domain treats them as insignificant. Choose explicit numeric culture/decimal policy; distinguish integer from decimal forms, bound length before expensive processing and reject invalid/non-finite/overflowing numbers.

Validate ranges, identifiers and cross-field constraints through one authoritative domain source where possible; client checks provide feedback. Authorization, replay/abuse and trust boundaries belong to [security](../security-and-validation/SKILL.md). Use structured APIs for rich text, paths, URLs, shell and SQL rather than unsafe concatenation.

Return useful localized errors without hidden state or internal exceptions. Variables/types must match the selected FTL contract under [locale policy](../../rules/content-and-localization.md); technical IDs are acceptable only where the diagnostic audience requires them, not as unexplained player labels.

For numeric/text format changes read [numeric-and-text-input](references/numeric-and-text-input.md). Select checks for the affected empty/boundary/normalization/culture/cross-field case under [verification](../../rules/verification.md), not a compulsory full matrix.
