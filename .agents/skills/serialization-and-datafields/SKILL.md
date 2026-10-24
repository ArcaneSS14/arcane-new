---
name: serialization-and-datafields
description: Design prototype DataFields, generated component state, custom serializers, and compatibility-safe data contracts.
---
# serialization-and-datafields

## Когда применять

Use for DataFields, serialized configuration, runtime state or compatibility contracts.
Use for DataFields, serialized configuration, runtime state or compatibility contracts.

## До изменения

Find declaration, YAML consumer, defaults, nullability, serializers and every existing consumer.
Find declaration, YAML consumer, defaults, nullability, serializers and every existing consumer.

## Правила и границы

Separate prototype configuration, serializable runtime state and server-only implementation detail. A DataField name/type/default/nullability is a contract.
Use DataField for declared serialized/prototype configuration and other values whose consumers need a data contract; defaults must be valid before prototype application. Prefer typed IDs and structured data, and keep runtime caches, services and derived state out of the contract.
Use DataField for declared serialized/prototype configuration and other values whose consumers need a data contract; defaults must be valid before prototype application. Prefer typed IDs and structured data, and keep runtime caches, services and derived state out of the contract.

### Minimal examples

Correct: use DataField for author-configurable serialized value with valid default. Prohibited: serialize runtime cache/service state. Stop: report unknown serializer/compatibility contract.

## Выбор подхода

Use a DataField when a serialized contract has a real producer and consumer; keep derived/cache/service values runtime-only; use custom serialization only when standard structured types cannot represent the contract.
Check required fields at prototype load or component startup. Use generated component state and the owner dirtying path for client-visible state.
Check required fields at prototype load or component startup. Use generated component state and the owner dirtying path for client-visible state.

## Антипаттерны

Do not rename/remove serializable fields without consumer search, or create hidden migrations through accidental fallbacks/defaults.
Do not rename/remove serializable fields without consumer search, or create hidden migrations through accidental fallbacks/defaults.

## Проверка

Check compatibility, prototype/maps consumers, null/default behavior and focused serialization validation.
Custom serializers need round-trip and invalid-input coverage.
Custom serializers need round-trip and invalid-input coverage.

## References

- [DataField patterns](references/datafield-patterns.md): Read this when adding or changing a DataField, serialized prototype value, default, collection, or compatibility-sensitive field.
- [Network serialization](references/network-serialization.md): Read this when designing or changing a network payload, component state, entity reference conversion, or replicated message compatibility.
