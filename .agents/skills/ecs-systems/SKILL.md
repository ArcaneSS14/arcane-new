---
name: ecs-systems
description: Implement entity behavior, subscriptions, lifecycle handling, and reusable action APIs.
---

## Когда применять
Use for behavior and mutation; do not create a system when an existing owner API fits.
## До изменения
Locate state owner, entry event, APIs, dependency boundary, query scope, lifecycle callbacks, cleanup, callers and test owner.
## Правила и границы
Systems own behavior. Handlers route, validation does not mutate, and execution has one mutation path. Dependencies and subscriptions follow system lifecycle.
Resolve dependencies through the established dependency mechanism; subscribe in initialization and release external callbacks on shutdown. Keep handlers thin, route reusable operations through Try methods, and keep Can checks mutation-free.
## Выбор подхода
Use direct owner API for one owner, an event for established extensibility, and bounded queries for matching entities.
Prefer existing entity helpers and bounded EntityQueryEnumerator-style queries over repeated global searches. Use guarded component lookup for optional state, resolution only where the caller contract requires it, and idempotent component creation only when creation is intended.
## Антипаттерны
- Multiple mutation paths cause duplicate effects.
- Broad hot-path scans cause performance regressions.
- Uncleaned callbacks mutate stale state.
- Immediate removal during dispatch, iteration, or physics callbacks can invalidate the active path; use the established deferred-removal path when that risk exists.
## Проверка
Cover repeated input, initialization, shutdown, deletion and the narrowest owner test.
Explicitly cover cleanup of callbacks, timers, overlays and cached entity references.
## References
- [System event patterns](references/system-event-patterns.md): Read this when designing an event handler, reusable action API, validation method, or execution path.
