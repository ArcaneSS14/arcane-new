---
name: minds-roles-and-objectives
description: Work with minds, sessions, bodies, roles, antagonists, objectives, ownership, and transfers safely.
---

## Когда применять

Use for minds, sessions, bodies, roles, antagonists, objectives, ownership, transfers, or round cleanup.

## До изменения

Identify whether state belongs to account/session, mind, current body, role, or entity component; trace server owner, serialized/prototype contract, visibility, transfer/deletion lifecycle, and existing test path.

## Правила и границы

- Session, mind, and controlled entity are related but not interchangeable. Store state at the layer matching its lifetime; re-resolve current body for authoritative actions.
- Handle cloning, polymorph, ghosting, reconnect, possession, administrative transfer, death, disconnect, role/objective completion/removal, round end, and mind deletion.
- Assign through existing role/objective systems; prevent duplicate grants and restrict hidden role/objective data to intended viewers.
- Clean actions, components, permissions, subscriptions, and references tied to a prior role/body. Server owns protected assignment/transfer outcomes.
- Stop at a missing owner API: do not use reflection, engine edits, direct cross-owner mutation, or a parallel mind/role system.

## Выбор подхода

Use the owner whose lifetime matches the feature and established transfer/role/objective API. Keep body-local presentation distinct from mind/session identity.

## Антипаттерны

- Do not cache a body as permanent identity, duplicate assignment, leak hidden objectives, or preserve stale role permissions after transfer.

## Проверка

When affected, check late join, reconnect, body transfer, cloning, ghosting, objective completion, role removal, duplicate assignment, and round cleanup through existing owner coverage.

## References

No reference is required for ordinary mind/role routing; inspect the existing owner and transfer lifecycle before changing assignment.
