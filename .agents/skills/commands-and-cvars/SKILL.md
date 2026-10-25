---
name: commands-and-cvars
description: Implement commands and CVar-backed configuration with validation, authority, persistence, localized feedback, and compatibility.
---

# commands-and-cvars

## Когда применять

Use for console commands, CVar-backed operational configuration, command permissions, runtime settings, or CVar compatibility changes.

## До изменения

Find current command registration/shell pattern, server authority/permission owner, exact CVar key, definition, flags, consumers, persistence, and runtime-change lifecycle.

## Правила и границы

- Separate parsing, authorization, validation, execution, and feedback. Server checks privileged commands; client completion/visibility is not authority. Audit player/round/persistence/config mutations.
- Validate argument count, parsing, ranges, non-finite values, and current state before mutation. Localize player-facing descriptions/help/errors in `en-US`; add Russian only on request. Keep internal exceptions, secrets, and unauthorized data out of player output. A permission-checked diagnostic/admin command may display technical IDs when they are part of its intended contract; do not replace useful diagnostics with player prose.
- Use a CVar only for global operational/runtime configuration, not per-entity/content state. Use an existing typed `CVarDef<T>` pattern, stable namespaced key, safe typed default, units where needed, and flags verified in the current engine.
- CVar keys are global operational APIs. Renames need compatibility/migration. Subscribe only for supported live changes; match subscription lifetime to its owner and handle invalid/removed persisted values safely.
- Stop at a missing supported command/CVar extension point: do not bypass access, use reflection, alter engine, or create a parallel configuration system.

### Source ledger

Claim: CVar definition/flag behavior must be verified in the current engine rather than inferred.
Source: Current branch CVar consumers and pinned RobustToolbox declarations.
Source type: Arcane / RobustToolbox
Checked against branch: `66cf1fb0a7`
Volatility: version-bound
Recheck trigger: Changing CVar flags, engine revision, or replication behavior.

### Minimal examples

Correct: validate command input and permission at its server owner. Prohibited: use a CVar for per-entity state. Stop: report unverified CVar flags or command registration.

## Выбор подхода

Use a component/prototype for content/entity state and a CVar for global behavior. Define replicated values in Shared only when replication is required; never store secrets/private server state in a CVar.

## Антипаттерны

- Do not broaden permissions, trust client values, use `CONFIDENTIAL` as secret storage, or leave live subscriptions after owner shutdown.
- Do not invent CVar flags or commands from another fork.

## Проверка

When changed, cover command parsing/bounds/permissions/output or CVar default/override/live update/replication/archive/invalid fallback as applicable, with an existing owner project/test path only.

## References

- [CVar change checklist](references/cvar-checklist.md): Read this when adding, renaming, or changing a CVar's type, default, flags, or runtime behavior.
