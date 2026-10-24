---
name: ecs-components
description: Design SS14 components as serialized state in the correct assembly.
---

# ecs-components

## Когда применять

Use for entity state; not for behavior, service lookup, or hidden ownership.
Use for entity state; not for behavior, service lookup, or hidden ownership.

## До изменения

Find schema, prototype, state owner, serializers, consumers, assemblies, and replication/presentation when clients consume data.
Find schema, prototype, state owner, serializers, consumers, assemblies, and replication/presentation when clients consume data.

## Правила и границы

Components contain serializable data. Shared holds only cross-side data; hidden authority remains Server; client-only types never enter Shared.
Use the repository's component registration and serialization attributes. Use prototype-backed data fields for author-tunable configuration, typed prototype IDs where the current schema supports them, and defaults that remain valid before prototype data is applied.
Use the repository's component registration and serialization attributes. Use prototype-backed data fields for author-tunable configuration, typed prototype IDs where the current schema supports them, and defaults that remain valid before prototype data is applied.

### Minimal examples

Correct: store serialized component state in the assembly that owns its authority. Prohibited: put runtime cache in prototype DataFields. Stop: report an unverified component/state contract.

## Выбор подхода

Use a component field for durable entity state, prototype data for author tuning, and system-local state for implementation detail.
Put a component in Shared only when both sides need its contract or replicated state; use Server for authoritative-only data and Client for presentation-only caches. A client-visible server mutation must use the established generated state and dirtying path owned by its system.
Put a component in Shared only when both sides need its contract or replicated state; use Server for authoritative-only data and Client for presentation-only caches. A client-visible server mutation must use the established generated state and dirtying path owned by its system.

## Антипаттерны

- Logic or service references in components break lifecycle and serialization.
- Replicating hidden state leaks authority.
- Mutating replicated fields outside the owner path leaves clients stale.
- Mutable collections without an explicit serialization and dirtying strategy produce stale or shared state.
- Mutable collections without an explicit serialization and dirtying strategy produce stale or shared state.

## Проверка

Check defaults, serialization, prototype contract, owner mutation and relevant consumers.
Check defaults, serialization, prototype contract, owner mutation and relevant consumers.

## References

- [Component patterns](references/component-patterns.md): Read this when designing component fields, prototype configuration, entity references, or replicated runtime state.
