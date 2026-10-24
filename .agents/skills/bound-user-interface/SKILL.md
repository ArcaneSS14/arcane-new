---
name: bound-user-interface
description: Implement BUI contracts, server validation, localized client windows, lifecycle cleanup, and state refresh.
---
# bound-user-interface

## Когда применять

Use BUI for entity-owned UI; use EUI for session-oriented non-entity UI.
Use BUI for entity-owned UI; use EUI for session-oriented non-entity UI.

## До изменения

Trace the Shared UI key and serializable contract, server owner/handler/state, client adapter, XAML window, and open/close/deletion/range-loss lifecycle. Establish which framework checks already exist and what the server path must still check.
Trace the Shared UI key and serializable contract, server owner/handler/state, client adapter, XAML window, and open/close/deletion/range-loss lifecycle. Establish which framework checks already exist and what the server path must still check.

## Правила и границы

Shared contains only the UI key and minimal serializable intent/state contract. Messages express intent, not a claimed result. The server validates actor, lifecycle, range, access, and current state, performs authoritative mutation, and refreshes minimal state without hidden data or resolved text. The client adapter opens/closes the window, applies state, and forwards typed intent; XAML owns controls/view state, never authorization or gameplay outcome.

Subscribe once for a window lifetime. Unsubscribe from every long-lived publisher on close or dispose, and prevent duplicate subscriptions after reopen. For multiple viewers, keep per-viewer presentation state separate while the server refreshes only viewer-appropriate shared state. A delayed callback must verify the identity of the current window and owner before changing controls. Refresh culture-sensitive text without discarding or replacing authoritative state.
Subscribe once for a window lifetime. Unsubscribe from every long-lived publisher on close or dispose, and prevent duplicate subscriptions after reopen. For multiple viewers, keep per-viewer presentation state separate while the server refreshes only viewer-appropriate shared state. A delayed callback must verify the identity of the current window and owner before changing controls. Refresh culture-sensitive text without discarding or replacing authoritative state.

### Minimal examples

Correct: send typed intent and validate actor/range/state on Server. Prohibited: authorize from a client button. Stop: report when the BUI contract or server owner is unavailable.

## Выбор подхода

Use one source for displayed state: the latest replicated component or BUI state. Keep window selection/filter presentation-only unless the feature contract explicitly requires otherwise; per-viewer presentation state never substitutes shared entity state.
Use one source for displayed state: the latest replicated component or BUI state. Keep window selection/filter presentation-only unless the feature contract explicitly requires otherwise; per-viewer presentation state never substitutes shared entity state.

## Антипаттерны

Do not use client button state as authorization, send a client-supplied result, keep competing displayed-state copies, or allow a callback to update a closed or rebound window.
Do not use client button state as authorization, send a client-supplied result, keep competing displayed-state copies, or allow a callback to update a closed or rebound window.

## Проверка

Cover open/close/reopen, deletion, range loss, viewers, stale state, delayed callbacks and culture refresh.
Cover open/close/reopen, deletion, range loss, viewers, stale state, delayed callbacks and culture refresh.

## References

- [BUI flow](references/bui-flow.md): Read this when adding or reviewing a BUI open, state, intent-message, validation, or close path.
- [Testing matrix](../testing/references/testing-matrix.md): Read this when selecting BUI lifecycle, message-flow, or reconnect coverage.
