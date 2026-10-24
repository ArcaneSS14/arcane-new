---
name: entity-lifecycle-and-spawning
description: Spawn, initialize, replace, transfer, and delete entities without breaking lifecycle, transforms, containers, or ownership.
---
## Когда применять
Use when entities spawn, initialize, move between owners, replace or delete; not for a presentation-only change.
## До изменения
Trace prototype, placement coordinate space, parent/container, initialization, references, cleanup, replication and owner test.
## Правила и границы
Use established owner APIs and prototype composition. Define preservation/reset semantics for replacement. Cleanup timers, UI, actions, children and cached references.
Choose spawn semantics deliberately: attachment for a parent-owned entity, container insertion for contained ownership, grid placement for grid-local placement, and map coordinates for map placement. Do not manually reconstruct a prototype component set unless runtime composition is the intent.
### Minimal examples

Correct: choose spawn semantics for map/grid/container ownership. Prohibited: delete twice without idempotent cleanup. Stop: report unverified initialization/transfer contract.

## Выбор подхода
Choose spawn semantics from attachment/container/grid/map intent; choose replacement only after listing state to preserve.
For replacement or transfer, explicitly decide position/rotation, parent/container membership, mind/session/ownership, hands/inventory, damage/status/actions, metadata/appearance, BUI/EUI sessions, timers/external references and persistence identity.
## Антипаттерны
- Manual prototype reconstruction diverges from content.
- Cached EntityUid without deletion strategy becomes stale.
- Client spawning authoritative gameplay duplicates state.
- Assuming map initialization or optional components without tracing the actual lifecycle causes partial initialization.
## Проверка
Cover placement, containers, init, replacement, deletion, transfer and cleanup.
Handle repeated deletion idempotently and clean dependent entities, sessions, timers, overlays and cached references.
## References
- [Replacement checklist](references/replacement-checklist.md): Read this when replacing, transforming, respawning, or transferring an entity while preserving selected state.
