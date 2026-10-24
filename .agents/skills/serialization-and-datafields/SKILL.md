---
name: serialization-and-datafields
description: Maintain serialized fields, defaults, collection merge rules and custom formats for prototypes, maps and stored data. Use networking for transport and component state delivery.
---

# Serialized fields and compatibility

Identify the actual loader/format: prototypes, maps, save/config state or another serializer. DataFields are not limited to designer tuning; expose values the actual format needs, with valid defaults, meaningful nullability, typed IDs and appropriate bounds. Keep service references, unintended caches and derived runtime state out of persisted data.

Choose existing serializers and project validation patterns before adding a custom serializer. Establish field name/type compatibility with existing consumers/data; renames/removals may need migration or aliases. Collections have explicit merge/replacement semantics, and mutable defaults must not be accidentally shared between instances.

Check required/default fields at the correct load/startup point. For a custom format exercise round-trip and invalid inputs that distinguish its contract. Select checks under [verification](../../rules/verification.md).

Component registration/state ownership belongs to [ecs-components](../ecs-components/SKILL.md); generated/manual transport, entity conversion and dirtying belong to [networking](../networking/SKILL.md). Avoid a second network-state procedure here.
