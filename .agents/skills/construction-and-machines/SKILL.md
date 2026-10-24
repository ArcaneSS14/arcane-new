---
name: construction-and-machines
description: Implement construction graphs, machine lifecycle, power, parts, upgrades, examination, and machine UI.
---

## Когда применять

Use for construction graphs, machines, boards/parts, upgrades, power/anchoring, examination, or machine UI.

## До изменения

Trace graph/node conventions, prototype contract, machine owner, map initialization, anchoring/power/damage/deconstruction lifecycle, part rating, UI, and targeted owner test.

## Правила и границы

- Keep construction data-driven; validate tools/materials, replacement, deconstruction, and state transfer. Do not import source-fork graph IDs.
- Subscribe to lifecycle events rather than polling each machine. Define map spawn, anchoring, power loss, damage/breakage, deconstruction, deletion, and cleanup.
- Use existing board/part-rating APIs. Recalculate derived values at the established part-change event; defaults work without upgrades and every cache has invalidation.
- Server owns machine state, costs, inventory/output, and access. BUI expresses intent and is revalidated; expose minimal UI state.
- Stop without a supported extension point: do not bypass access, reflect, change engine, or create a parallel machine system.

## Выбор подхода

Extend the current graph/machine owner and prototype fields; use event-driven recalculation and existing BUI flow instead of isolated loops/caches.

## Антипаттерны

- Do not lose contents/ownership on replacement, run unpowered/unanchored machinery, trust client price/output, or let map-spawn skip setup.
- Do not leave examine text inconsistent with authoritative state.

## Проверка

When affected, check construction/deconstruction, map spawn, power loss, anchoring, part replacement, damage, UI concurrency, and targeted YAML/owner behavior.

## References

- [Machine lifecycle](references/machine-lifecycle.md): Read this when changing a machine's spawn, anchoring, power, part, damage, deconstruction, or UI lifecycle.
