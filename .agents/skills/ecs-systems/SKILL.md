---
name: ecs-systems
description: Implement entity behavior, subscriptions, lifecycle handling, and reusable action APIs.
---

# ecs-systems

## Когда применять

Use for behavior and mutation; do not create a system when an existing owner API fits.
Use for behavior and mutation; do not create a system when an existing owner API fits.

## До изменения

Locate state owner, entry event, APIs, dependency boundary, query scope, lifecycle callbacks, cleanup, callers and test owner.
Locate state owner, entry event, APIs, dependency boundary, query scope, lifecycle callbacks, cleanup, callers and test owner.

## Правила и границы

Systems own behavior. Handlers route, validation does not mutate, and execution has one mutation path. Dependencies and subscriptions follow system lifecycle.
Resolve dependencies through the established dependency mechanism. For a fixed handler compatible with the current generator, `[SubscribeLocalEvent]` may replace a matching manual registration; retain an explicit subscription when its conditional/dynamic lifetime or other contract cannot be expressed by the generator, and never use both forms. `[Dependency]` fields require a partial containing type and must not be readonly; injected `EntityQuery<T>` is supported when a cached query is needed. Keep handlers thin, route reusable operations through Try methods, and keep Can checks mutation-free.

### Source ledger

Claim: The current pinned engine supports generated local-event subscriptions and lazy `EntityQuery<T>` dependency injection; dependency analyzer rules require partial containing types and non-readonly injected fields.
Source: `Robust.Shared/Analyzers/EntitySystemSubscriptionsGeneratorAttributes.cs`, `Robust.Shared/GameObjects/EntitySystemManager.cs`, `Robust.Analyzers/HasDependenciesAnalyzer.cs`, and current Arcane system usages.
Source type: RobustToolbox / Arcane
Checked against branch: `AI-skills` at `8cfe7ef937`
Volatility: version-bound
Recheck trigger: Changing the RobustToolbox pin, analyzer package, or ECS subscription generator.

### Minimal examples

Correct: use one generator or explicit subscription path that preserves the required lifecycle/order. Prohibited: register the same handler both ways. Stop: report an unsupported handler signature or unclear lifecycle/order contract.

## Выбор подхода

Use direct owner API for one owner, an event for established extensibility, and bounded queries for matching entities.
Prefer existing entity helpers and bounded EntityQueryEnumerator-style queries over repeated global searches. Use guarded component lookup for optional state, resolution only where the caller contract requires it, and idempotent component creation only when creation is intended.
Prefer existing entity helpers and bounded EntityQueryEnumerator-style queries over repeated global searches. Use guarded component lookup for optional state, resolution only where the caller contract requires it, and idempotent component creation only when creation is intended.

## Антипаттерны

- Multiple mutation paths cause duplicate effects.
- Broad hot-path scans cause performance regressions.
- Uncleaned callbacks mutate stale state.
- Immediate removal during dispatch, iteration, or physics callbacks can invalidate the active path; use the established deferred-removal path when that risk exists.
- Immediate removal during dispatch, iteration, or physics callbacks can invalidate the active path; use the established deferred-removal path when that risk exists.

## Проверка

Cover repeated input, initialization, shutdown, deletion and the narrowest owner test.
Explicitly cover cleanup of callbacks, timers, overlays and cached entity references.
Explicitly cover cleanup of callbacks, timers, overlays and cached entity references.

## References

- [System event patterns](references/system-event-patterns.md): Read this when designing an event handler, reusable action API, validation method, or execution path.
