---
name: entity-relations-and-links
description: Maintain entity references, ownership links, target relationships, deletion cleanup, and network-safe relation state.
---

## Когда применять

Use for entity ownership links, targets, attachments, relation components, network-safe references, and lifecycle cleanup of source/target relationships.

## До изменения

Determine the relation owner, direction, cardinality, authority, serialization/network form, and cleanup owner. Trace source/target lifecycle through deletion, component removal, container/body/map transfer, reconnect, and persisted restore.

## Правила и границы

- Use nullable entity IDs, network-safe entity forms, or an existing relation component. Never store direct component/object references as durable relation state.
- Define whether the contract is one-way, bidirectional, unique, or many-to-many. If both sides store it, update both through one owning system or an atomic path, and prevent stale back-references.
- The server validates arbitrary client-supplied targets and owns protected relationships such as account ownership, machine control, role assignment, and target locks.
- Replicate only relation targets needed for presentation. Do not expose hidden target relationships merely because a client receives a related entity.
- Handle source/target deletion, component removal, container/body/map transfer, reconnect, and invalid persisted restoration through the designated cleanup owner or revalidation before use.

### Minimal examples

Correct: update both sides through the single relation owner. Prohibited: retain direct object references. Stop: report undefined cardinality or cleanup ownership.

## Выбор подхода

Use an existing relation component or network-safe entity form when it matches the owner contract. Use one-way storage for a one-way relationship; use a single owning system/atomic update for bidirectional links; use a uniqueness-aware owner for unique or many-to-many relations.

## Антипаттерны

- Do not retain direct object/component references, duplicate cleanup ownership, or leave a deleted target cached.
- Do not trust a client-selected linked entity or replicate a hidden target without presentation necessity.
- Do not preserve a relation through transfer when its contract requires reset, or restore an invalid entity ID without validation.

## Проверка

Check link, relink, unlink, duplicate request, source/target deletion, component removal, container/body/map transfer, reconnect, invalid restore, and stale back-reference cleanup.

## References

No reference is required for ordinary relation routing; inspect the existing relation owner and its cleanup path before changing a link contract.
