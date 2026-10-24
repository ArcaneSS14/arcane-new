---
name: ecs-events
description: Choose and implement local, directed, broadcast, cancellable, and network events.
---

# ecs-events

## Когда применять

Use for established ECS flow or real decoupling; not to hide a direct call to one owner.
Use for established ECS flow or real decoupling; not to hide a direct call to one owner.

## До изменения

Find publisher, subscribers, assembly boundary, state owner, ordering/cancellation contract and any prediction/network path.
Find publisher, subscribers, assembly boundary, state owner, ordering/cancellation contract and any prediction/network path.

## Правила и границы

Directed events target entity components; broadcasts notify independent listeners; mutable/cancellable events require an explicit result owner. Network events are minimal intent, never client results.
Define whether the event is notification-only, before/after mutation, mutable, handled or cancellable. The publisher owns the ordering contract and the cancellation/result semantics.
Define whether the event is notification-only, before/after mutation, mutable, handled or cancellable. The publisher owns the ordering contract and the cancellation/result semantics.

### Minimal examples

Correct: use a directed event when one owner handles a contract. Prohibited: use broadcast to bypass owner API. Stop: report unknown cancellation/order/authority semantics.

## Выбор подхода

Use direct calls for one owner, local events for component reactions, broadcasts for observers, and network contracts only for a real boundary.
At a network boundary, verify prediction behavior and keep the authoritative decision and validation on the server even if a Shared handler participates.
At a network boundary, verify prediction behavior and keep the authoritative decision and validation on the server even if a Shared handler participates.

## Антипаттерны

- Events as hidden state lose a source of truth.
- Undefined cancellation/order causes accidental behavior.
- Server-only data or claimed success crosses a trust boundary.
- Server-only data or claimed success crosses a trust boundary.

## Проверка

Check subscribers, ordering, cancellation, repeat execution and server validation.
Check subscribers, ordering, cancellation, repeat execution and server validation.

## References

No reference is required for ordinary event selection.
