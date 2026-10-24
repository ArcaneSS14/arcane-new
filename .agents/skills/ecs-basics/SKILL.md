---
name: ecs-basics
description: Read and reason about an unfamiliar SS14 ECS subsystem before changing it.
---

## Когда применять

Use for an unfamiliar ECS feature or cross-system bug; not instead of a narrow domain skill.
## До изменения
Trace entry point, prototype, component state, owner system, events, Server/Shared/Client path, replication/presentation, and nearest owner test.
1. Find the primary component and the prototype that adds it.
2. Find systems and lifecycle/event subscriptions for that component.
3. Trace the player or simulation entry point.
4. Follow validation, mutation, dirtying and presentation.
5. Establish Server, Shared, Client and Resources ownership.
6. Find the nearest owner-local or integration test.
## Правила и границы
Entities identify; components store state; systems own behavior and mutation; prototypes compose initial state.
Ask: which component stores state; which system may mutate it; which event starts the flow; what is authoritative, predicted and replicated; which lifecycle initializes or cleans up; and which prototype, resource and locale dependencies are required.
### Minimal examples

Correct: trace component, system, event, mutation, dirtying, and presentation. Prohibited: edit the first matching component without flow discovery. Stop: report unknown owner/lifecycle before changing it.

## Выбор подхода
Choose the existing state owner first, then its system and event; trace client presentation separately from authority.
## Антипаттерны
- Copying a system without matching lifecycle causes wrong ownership and cleanup.
- Treating presentation as authority hides server validation.
## Проверка
Verify the complete flow and the narrowest affected test.
## References
- [ECS reading guide](references/ecs-reading-guide.md): Read this when mapping an unfamiliar feature from prototype or component to events, presentation, and tests.
- [Lifecycle primer](references/lifecycle-primer.md): Read this when choosing initialization, cleanup, deletion, parent-change, anchoring, container, or map-transfer handling.
