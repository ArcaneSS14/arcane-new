---
name: eui
description: Implement EUI sessions, shared state and messages, server lifecycle, and client windows.
---

## Когда применять

Use EUI for a session-oriented interface not owned by one entity: a menu, lobby, administration, or separate client/server session flow. Use BUI for an entity-owned UI.

## До изменения

Find the server-session owner, Shared message/state contract, viewer admission rules, client window, and every open, update, and close path.

## Правила и границы

The server creates and closes the session, checks viewer authorization before opening it, and sends only data that viewer may see. Each message revalidates sender, permission, target, operation, and current state; disconnect, permission change, target deletion, and shutdown must close or invalidate the session. The client applies the latest state, releases subscriptions on close, and ignores callbacks from an obsolete session. A visible control or enabled button is never authorization.

### Minimal examples

Correct: validate each session message on Server. Prohibited: treat client permission as authorization. Stop: report missing session lifecycle/visibility owner.

## Выбор подхода

Choose BUI when lifecycle and authority naturally belong to an entity; choose EUI when a server session, rather than a world object, owns them. Keep the Shared contract minimal and mutation on Server.

## Антипаттерны

Do not trust client-side permission, publish hidden viewer data, or retain a window or subscription after its session closes.

## Проверка

Check open, state update, rejection, close/reopen, disconnect, permission change, target deletion, and stale-update rejection; select focused lifecycle coverage at the owner.

## References

- [Testing matrix](../testing/references/testing-matrix.md): Read this when selecting EUI lifecycle, message-flow, rejection, disconnect, or stale-session coverage.
