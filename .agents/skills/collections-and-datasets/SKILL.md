---
name: collections-and-datasets
description: Maintain weighted collections, datasets, groups, selectors, and random content safely.
---
# collections-and-datasets

## When to use

Use for data-driven collections, datasets, groups and selectors.

## Before editing

Find owner, consumer, existing loader/selector and every referenced prototype/resource.

## Rules and boundaries

Distinguish weighted, unique, ordered, nested and filtered entries. Protected random outcomes are server-authoritative; predicted random must use a deterministic source or authoritative correction. Player-visible dataset strings require FTL.
Check empty-set, duplicate-ID, fallback, nesting, and deterministic/authoritative contracts against the existing loader and consumer.

### Minimal examples

Correct: select protected weighted outcome on the authoritative owner. Prohibited: let a client choose protected random data. Stop: report an unverified collection loader or fallback contract.

## Choose an approach

Reuse the existing loader/selector; keep data-driven selection in data instead of introducing runtime randomness when content owns it.

## Avoid

Do not leave invalid references, implicit fallback behavior, duplicate entries, or uncontrolled random outcomes.
Do not let a client select rewards, damage, targets or other protected outcomes.

## Verification

Check normalization, empty/fallback behavior, recursion, invalid references, and distribution-sensitive balance changes.
Use targeted tests for custom selection logic.

## References

No reference is required for ordinary collection selection.
