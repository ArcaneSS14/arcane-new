---
name: timers-and-async
description: Manage timers, cancellation, entity lifetime, asynchronous work, and main-thread state safely.
---

# timers-and-async

## Когда применять

Use for timers, delayed callbacks, cancellation, asynchronous I/O/computation, deferred ECS mutation, and work that can outlive an entity, session, map, or round.

## До изменения

Identify the timer/task owner, cancellation owner, entity/session/map/round lifetime, thread boundary, repeated-start behavior, and desired completion contract. Determine whether the owner already uses an update loop or shared scheduler before scheduling new work.

## Правила и границы

- Before every delayed mutation, re-check entity existence, component state, authority, and operation currency. Do not capture stale component references, target IDs, permission, or prices without revalidation.
- Use the established timing/scheduler pattern. Do not create a timer per entity in a hot system when the owner uses an update loop or shared scheduler.
- Background work is only for explicitly supported isolated I/O or computation. ECS mutation stays on the correct simulation thread; marshal a background result through the existing mechanism and validate it again before applying.
- Cancel or invalidate work at entity deletion, component removal, disconnect, map unload, round restart, and system shutdown when that lifetime ends. Define duplicate scheduling behavior so repeated starts cannot apply an effect twice.
- Handle exceptions at the owning boundary: a faulted callback must not leave an operation or cleanup state permanently active.
- Use `async void` only for a framework-required event handler that delegates to an observable task path. Do not block a simulation/UI thread with `Task.Wait`, `.Result`, or equivalent waiting unless the current owner API explicitly requires it and its lifetime/deadlock behavior is proven.

### Minimal examples

Correct: revalidate owner state before delayed mutation. Prohibited: block simulation with Task.Wait. Stop: report unavailable scheduler/thread handoff.

## Выбор подхода

Prefer an owner update loop for hot, repeated simulation work; use the existing shared scheduler/timer pattern for delayed completion; use supported background work only when it does not touch ECS state directly. Couple cancellation to the narrowest owner that can end the operation.

## Антипаттерны

- Do not let a callback mutate a deleted entity, ignore cancellation, or retain stale permission/target/price.
- Do not let a background thread access entity state or apply a result without returning to the simulation thread.
- Do not start duplicate timers that can both complete, or leave a repeating timer alive after system shutdown.

## Проверка

Check cancellation, duplicate scheduling, deletion, component removal, shutdown, disconnect, exception paths, and delayed completion after state/authority changes.

## References

- [Lifetime checklist](references/lifetime-checklist.md): Read this when a timer, callback, task, or delayed result may outlive an entity, session, map, round, or current state.
