---
name: containers-and-inventory
description: Move entities through containers, hands, slots, storage, equipment, and inventories without corrupting ownership or transforms.
---

# containers-and-inventory

## Когда применять

Use for containers, slots, hands, storage, equipment, pickup/drop, transfer, and inventory UI.

## До изменения

Identify the owning container/slot/hand/storage system, item lifecycle, capacity/whitelist/access/body constraints, transform/PVS behavior, UI state, and nearest owner test.

## Правила и границы

- Use established insertion/removal/pickup/drop/equip APIs. Never manually parent to simulate insertion or mutate container collections directly.
- Validate capacity, whitelist/blacklist, access, body state, and current ownership. Handle insertion/removal events and side effects exactly once.
- Server owns protected moves; drag-and-drop is intent only. Preserve transform/PVS and refresh UI from authoritative inventory state.
- Account for active hand, blocked slots, nested storage, deletion, body/polymorph transfer, disconnect, and multiple viewers.
- Stop when no supported owner API exists: do not bypass access, use reflection, alter engine, or build a parallel inventory implementation.

### Minimal examples

Correct: move an item with the owning container API. Prohibited: manually parent it as insertion. Stop: report when the destination owner or transfer invariant is unknown.

## Выбор подхода

Use the owner system that owns the destination and event flow; model transfers through its APIs, not an independent entity-parenting path.

## Антипаттерны

- Do not place an entity in two containers, skip removal events, leak contained entities through PVS, or retain invalid slots after deletion.
- Do not duplicate predicted feedback on hand transfer.

## Проверка

When affected, check insert/remove/swap/drop/delete, nested storage, full/denied inventory, body transfer, reconnect, and multiple viewers through existing owner coverage.

## References

- [Container invariants](references/container-invariants.md): Read this when moving, transferring, deleting, or replacing contained entities or inventory contents.
