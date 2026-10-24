---
name: collections-and-datasets
description: Maintain weighted collections, datasets, groups, selectors, and random content tables.
---

# Content collections

Find the prototype type and affected consumer. Determine whether entries are ordered, unique, weighted, nested or filtered; not every dataset is random. Preserve selection/order semantics, stable IDs, exact locale references and intended duplicates/weights.

Define empty/single-entry fallback and nested recursion behavior. Check a balance-sensitive weight change against normalization/consumer assumptions. Use [randomness-and-determinism](../randomness-and-determinism/SKILL.md) for authority, seed and replay rather than duplicating that contract.

Player-visible entries use [localization](../localization/SKILL.md). Select reference/schema checks or deterministic tests for custom selection logic under [verification](../../rules/verification.md); a data-only tuning change need not add new test infrastructure.
