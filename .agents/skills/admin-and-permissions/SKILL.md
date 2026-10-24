---
name: admin-and-permissions
description: Implement privileged commands, admin UI, role checks, audit logs, and safe operational controls.
---

## Когда применять

Use for privileged commands, admin UI, role/flag checks, player-affecting operations, and audit trails.

## До изменения

Identify the exact permission/flag, server entry points (command, EUI/BUI, verb, network event), target lifecycle, round/session state, and audit owner.

## Правила и границы

- Privileged tooling fails closed and is server-authoritative. A visible button or client state is never authorization; recheck permission at every server entry point.
- Keep read-only diagnostics separate from mutation. Validate target, ownership, session/round state, and current permission before each protected operation.
- Log actor, operation, target, safe important parameters, and outcome for high-impact changes; never log credentials, tokens, private chat, or unnecessary personal data.
- Return localized player-safe feedback without secrets. Use normal owner APIs so admin work preserves lifecycle and invariants.
- Stop when required access has no supported extension point: do not bypass modifiers, use reflection, alter engine, or create a parallel admin framework.

### Minimal examples

Correct: validate the exact server-side permission before an admin mutation. Prohibited: treat a visible admin button as authorization. Stop: report when no supported permission or audit owner exists.

## Выбор подхода

Use the narrowest existing permission instead of broadening a flag because a nearby command does. Route mutating functionality through its owner system and an auditable server handler.

## Антипаттерны

- Do not trust a client-selected target/result, bypass owner APIs, or treat command registration as a permission check.
- Do not log secrets or make normal rejection an error.

## Проверка

When privilege behavior changed, cover authorized/unauthorized users, missing session, disconnected target, repeated execution, malformed arguments, and runtime permission changes using the existing owner test path.

## References

- [Permission review](references/permission-review.md): Read this for a privileged command, admin UI, mutating admin operation, or permission-check change.
