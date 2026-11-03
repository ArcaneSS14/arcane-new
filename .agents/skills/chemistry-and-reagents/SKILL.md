---
name: chemistry-and-reagents
description: Modify reagents, solutions, reactions, transfers, metabolism, effects, containers, and chemistry UI safely.
---

# chemistry-and-reagents

## Когда применять

Use for reagent/reaction prototypes, solution transfer, metabolism, chemistry effects, containers, or chemistry UI.

## До изменения

Find the existing reagent/reaction/solution owner, prototype IDs, quantities, catalysts, conditions, products, effects, UI path, and targeted test/validation owner.

## Правила и границы

- Use existing reagent/reaction prototype types and solution-system APIs for add/remove/split/transfer/query; never mutate solution collections directly.
- Verify IDs, quantities, catalysts, temperature/conditions, products, side effects, insufficient volume, capacity, spill, deletion, and concurrent transfer semantics. Keep balance author-configurable in prototypes.
- Server owns reaction outcomes, metabolism, damage, and inventory changes; UI previews known data but does not choose results. Keep Shared solution state and presentation minimal.
- Define effect conditions, scaling, interval, cap, stacking, and cleanup. Avoid per-tick allocations or repeated costly lookup in metabolism.
- Stop at a missing supported owner API: do not use reflection, direct collection mutation, engine edits, or a parallel solution system.

### Minimal examples

Correct: transfer solution through the existing solution-system API. Prohibited: mutate reagent collections directly. Stop: report when the solution owner API cannot be established.

## Выбор подхода

Extend the existing solution/reaction/metabolism owner and prototype contract. Use owner-local UI refresh only as presentation of authoritative state.

## Антипаттерны

- Do not hardcode balance branches, expose reagent IDs as display text, or trust client-selected transfer/result.
- Do not let a deleted container or canceled transfer retain stale solution state.

## Проверка

When affected, check exact/boundary volume, reaction order, catalyst, temperature, repeated metabolism, transfer cancellation, and targeted prototype/owner validation.

## References

- [Solution safety](references/solution-safety.md): Read this when implementing or reviewing reagent transfer, solution mutation, capacity handling, or chemistry UI refresh.
