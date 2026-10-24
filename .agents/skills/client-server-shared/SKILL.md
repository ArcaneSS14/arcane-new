---
name: client-server-shared
description: Place contracts and behavior across authority, prediction, presentation, localization, and assembly boundaries.
---
## Когда применять
Use when behavior crosses Server, Shared, Client, UI, or prediction; not for owner-local behavior.
## До изменения
Trace entry, contract, authority owner, validation, mutation, consumers, replication, presentation and test owner.
Choose the layer from authority and real dependencies, never from the easiest project reference. Establish the actual client/server dispatch path before relying on framework behavior.
## Правила и границы
Server owns protected outcomes. Shared contracts are minimal and safe on both sides. Client presents intent/state; it never authorizes or commits outcomes.
Put serializable cross-boundary contracts in Shared, authoritative validation and mutation in Server, and windows, code-behind and presentation in Client. A Shared handler that also executes predictively must not perform irreversible or privileged work.
### Minimal examples

Correct: put serializable contract in Shared and protected mutation on Server. Prohibited: perform irreversible work in predicted shared handling. Stop: report an unproven assembly/authority boundary.

## Выбор подхода
Place serializable contracts in Shared, authoritative validation/mutation in Server, presentation in Client.
On the authoritative server path re-check authorization, actor ownership, value bounds and current state. Send requested intent, never a client-claimed result; omit hidden state and already-resolved localized text from network contracts.
For BUI/EUI, keep the window and code-behind in Client, the message/state contract in Shared, and authoritative mutation in Server.
## Антипаттерны
- Client validation as security boundary permits forged requests.
- Resolved text or hidden state in contracts leaks data and localization ownership.
## Проверка
Cover rejection, stale state, ownership and affected cross-layer path.
## References
- [Trust boundary](references/trust-boundary.md): Read this when adding or reviewing a client-originated message, request validation, or replicated response that exposes authority-sensitive state.
