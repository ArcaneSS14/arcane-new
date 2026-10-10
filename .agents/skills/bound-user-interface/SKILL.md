---
name: bound-user-interface
description: Implement BUI contracts, server validation, localized client windows, lifecycle cleanup, and state refresh.
---

A BUI adapts an entity-owned interface to gameplay state. Keep its responsibilities split across the layers that own them.

## Layer responsibilities

- Shared defines only the UI key and serializable intent/state contracts needed across the network. Messages express requested actions, not trusted results. Keep state minimal and exclude hidden or server-only data.
- Server validates every request when it is handled against the current actor, entity lifecycle, range, permissions, and current gameplay state; then it applies authoritative mutation and publishes refreshed state. Verify which actor, range, and lifecycle checks the BUI framework performs, and enforce any remaining checks in the server path. Never trust a client-supplied result, permission, or stale state snapshot.
- Client BUI creates and closes the window, applies incoming state, and forwards typed intent. Keep gameplay policy out of the adapter.
- The XAML window owns controls and view state. It reports input through events or narrow methods; it does not authorize actions or commit gameplay outcomes.

Keep payloads as small as practical. Prefer typed state localized by the client over resolved strings sent over the network. If a Shared handler also runs predictively, keep its effects deterministic and safe to reconcile.

## Lifecycle and state flow

- Create the window on open and release references on close. Subscribe once per window lifetime; unsubscribe from longer-lived publishers and prevent duplicate subscriptions after reopen.
- Handle owner deletion, range loss, multiple viewers, stale state, and reopen according to the actual UI framework lifecycle.
- Choose one source for displayed state. Refresh controls from the latest replicated component or BUI state; avoid a second stale copy in the adapter or window.
- Ensure a delayed response or callback cannot update a window after it has closed or rebound to another owner. Follow the framework's session/window identity and cancellation pattern.
- Keep per-viewer presentation state separate from shared entity state. A window's local selection or filter must not become authoritative gameplay state unless the feature contract explicitly requires it.
- Long-lived windows must refresh localized text on culture changes without discarding authoritative state. Every visible string requires `en-US` localization; add Russian only when explicitly requested.

Use repository ownership rules for edit markers. Do not mark owner-local module or underscore paths.

Verify the affected contract and lifecycle with the narrowest checks supported by the owning projects. Select only the lifecycle cases affected by the change, using `.agents/skills/testing/references/testing-matrix.md` and root `AGENTS.md`; do not restore dependencies or run every integration test by default.
