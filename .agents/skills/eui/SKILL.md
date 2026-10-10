---
name: eui
description: Implement EUI sessions, shared state and messages, server lifecycle, and client presentation.
---

Use EUI for session-oriented interfaces that are not naturally bound to one world entity or an existing BUI owner.

## Shared contract

Define only the serializable state and intent messages needed across the client/server boundary in Shared. Keep them minimal and free of hidden server-only data, UI controls, and server services. Messages describe requested intent, not a claimed result or authorization decision.

## Server lifecycle

The server creates and owns the session, checks authorization before opening it, sends only data the viewer may see, handles messages, revalidates the actor, permission, target, and requested operation for every message, and closes the session. Treat the client and its displayed state as untrusted. Handle disconnects, permission changes, target deletion, and explicit shutdown according to the actual EUI lifecycle.

## Client lifecycle

The client creates presentation, applies the newest session state, sends intent, and disposes windows and subscriptions on close. Do not treat client button state as authorization. Ignore or cancel delayed callbacks for a closed or superseded session so stale results cannot repopulate the wrong window.

## Choosing EUI or BUI

Use BUI when the interaction is clearly owned by an entity and normal range/lifecycle semantics fit. Use EUI for broader admin, debug, lobby, or session-oriented interfaces.

## Verification

Choose focused checks for the changed behavior. For lifecycle or message-flow changes, cover the relevant open/update/rejection/close/disconnect/stale-session cases; do not require every case for an unrelated presentation-only edit. Check affected localization and constrained text layout when those surfaces change. Follow root `AGENTS.md` and `.agents/skills/testing/references/testing-matrix.md` for verification scope.
