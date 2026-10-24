---
name: entity-api-patterns
description: Use owner APIs and established EntitySystem helpers without copying low-level logic.
---
## Когда применять
Use when selecting an entity, prototype, spawn, delete, popup, audio or query operation; not when an owner-specific API already defines the contract.
## До изменения
Find state owner, public accessible API, assembly, lifecycle and caller authority before selecting a helper.
## Правила и границы
Prefer the highest-level existing owner API. Verify declaration and access; preserve container, transform and network invariants through their owners.
Use existing component access helpers such as HasComp, TryComp, CompOrNull, Resolve and EnsureComp only with the surrounding caller's established semantics. Use typed prototype IDs; use Index when absence is a data error and TryIndex when absence is an expected branch.
### Minimal examples

Correct: call the existing owner API for component/entity operation. Prohibited: copy private owner logic. Stop: report inaccessible API with the extension boundary.

## Выбор подхода
Choose an owner API before generic helper; choose a query only for a bounded read; use deferred removal when dispatch or enumeration requires it.
Choose spawn helpers from attachment, container, grid or map placement intent rather than converting coordinates for convenience. Use the current owner API for delete, popup, audio and transform work instead of reconstructing its low-level logic.
## Антипаттерны
- Copying private/low-level logic bypasses invariants.
- Reflection or access workarounds cross assembly boundaries.
## Проверка
Check the target's lifecycle, authority and affected owner contract.
## References
No reference is required for ordinary owner-API selection.
