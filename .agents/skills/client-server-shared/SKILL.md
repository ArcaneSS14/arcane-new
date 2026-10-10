---
name: client-server-shared
description: Place contracts and behavior across authority, prediction, presentation, localization, and assembly boundaries.
---

Follow the layer ownership table in `.agents/rules/architecture-and-ownership.md`. Choose from actual dependencies and authority, not from the easiest project reference. Arcane-only behavior belongs in Arcane owner projects; base projects own reusable base behavior.

For client-originated messages, trace the actual dispatch path. Confirm framework guarantees from current declarations, then validate remaining authorization, ownership, value bounds, and state requirements on the authoritative server execution path before committing the outcome. A handler may live in Shared when it is safe on both sides; keep hidden or server-only decisions in Server. Shared handlers that also run predictively must not perform irreversible or privileged work.

Keep payloads minimal and represent requested intent rather than a claimed result. Replicate only state the client may know. Prefer typed state that the client localizes over resolved strings in network messages. Player-visible strings require `en-US` localization. Add or update `ru-RU` only when explicitly requested.

For UI flows, keep XAML and code-behind in Client, serializable cross-boundary contracts in Shared, and authoritative validation/mutation in Server. A BUI/EUI adapter connects the window to the state and message flow; it should not become a second gameplay system.

Before inherited-file changes, verify ownership, assembly boundaries, project references, and marker requirements. Choose verification for the changed behavior and follow root `AGENTS.md`; do not restore, build the full solution, or run every integration test by default.
