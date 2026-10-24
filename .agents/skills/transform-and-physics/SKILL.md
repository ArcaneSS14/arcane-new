---
name: transform-and-physics
description: Work with maps, grids, coordinates, anchoring, movement, fixtures, collisions, and physics safely.
---
## Когда применять
Use for coordinate, parent, anchoring, movement, fixture or collision changes; not for state that does not affect transform/physics.
## До изменения
Identify parent, map, grid, coordinate space, container state, anchoring, fixtures, authoritative owner and test path.
## Правила и границы
Use established transform and physics owner APIs. Keep coordinate spaces explicit; containers, anchoring and parent hierarchy affect valid movement and collision changes.
Identify whether the receiving API expects local, grid, map or world coordinates before converting. Do not directly mutate transform state when the transform owner API preserves parent, anchoring and container invariants.
## Выбор подхода
Select local, grid, map or world coordinates from the receiving API; resolve conversions deliberately before movement or spawning.
For physics, inspect the existing owner path for body type, fixtures, collision layers, masks, awake state and contact handling. Keep authoritative fixture and collision changes on the server/shared path used by nearby code.
## Антипаттерны
- Mixing coordinate spaces places entities incorrectly.
- Direct component mutation bypasses hierarchy/physics invariants.
- Client-only collision changes desync authority.
- Moving an entity while it is contained, or ignoring anchoring/parent changes, breaks transform hierarchy.
## Проверка
Cover grid/map edges, anchoring, containers, fixture/collision updates and deletion during events.
## References
No reference is required for ordinary transform or physics selection.
