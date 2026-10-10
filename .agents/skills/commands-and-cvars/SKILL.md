---
name: commands-and-cvars
description: Implement commands and CVar-backed configuration with validation, authority, persistence, localized feedback, and compatibility.
---

## Console commands

Keep parsing, authorization, validation, execution, and feedback distinct. Verify the current command and shell interfaces before implementation.

Commands affecting players, round state, persistence, or server configuration require permission checks and useful audit logging.

Command descriptions, help, usage, success, and player-safe errors must use `en-US`; add `ru-RU` only when explicitly requested. Do not expose internal exceptions or raw IDs.

Use the existing command registration and permission pattern. Validate argument count, parsing, ranges, and current world/player state before mutation. A client-visible command or command completion is not authorization; privileged actions must be checked on the server. Follow `admin-and-permissions` for permission and audit requirements when the command is privileged.

## CVars

Use a CVar only for a global operational or runtime configuration value. Per-entity/content state belongs in the relevant component or prototype, not a CVar. Before defining one, search the exact key: CVar names share a global namespace regardless of which definition class contains them. Use the current typed `CVarDef<T>` pattern, a stable namespaced key, safe typed default, description including units where needed, and explicit runtime behavior.

Choose flags from the value's actual ownership and lifecycle; verify their definitions in the current engine rather than treating them as interchangeable:

| Flag | Meaning to account for |
| --- | --- |
| `SERVER` / `CLIENT` | Which side is allowed to change the value |
| `SERVERONLY` / `CLIENTONLY` | Which side registers the value |
| `REPLICATED` | Synchronizes between client and server; define replicated CVars in Shared |
| `ARCHIVE` | Saves non-default values to the configuration file |
| `NOTIFY` | Server-side changes notify clients; use only for a Shared definition |
| `CHEAT` / `NOT_CONNECTED` | Restricts debug changes or changes while connected |
| `CONFIDENTIAL` | Hides the value from CVar command completion; it is not secret storage |

Decide whether the value is startup-only or supports runtime changes. Validate bounds and reject invalid/non-finite numeric values before applying timers, rates, sizes, economy values, or paths. Do not replicate private server state or put credentials/secrets in a CVar.

Subscribe only when live changes are supported. For an `EntitySystem`, prefer `Subs.CVar` so the subscription is removed with system lifecycle; use the immediate callback option when initialization must consume the already-loaded value. For other owners, pair subscription and unsubscription explicitly. Persisted client settings must survive restart and handle removed or invalid values safely.

CVar and command names are operational APIs. Renames require migration or compatibility handling.
Use `.agents/skills/commands-and-cvars/references/cvar-checklist.md` when adding or changing a CVar.

## Verification

Build only the affected project. When verification is requested, select focused tests for the changed behavior; do not restore dependencies or run the complete root or module test suites by default.

- Commands: cover parsing, missing/extra arguments, invalid and boundary values, permissions, and user-facing output when affected.
- CVars: cover default/override behavior, runtime updates, replication, archived persistence, or invalid-value fallback only where the change affects those contracts.
