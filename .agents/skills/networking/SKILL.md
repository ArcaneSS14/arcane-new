---
name: networking
description: Implement minimal network events and component state with authority, conversion, dirtying, and regression coverage.
---
## Когда применять
Use for replicated state, network intent, conversion or dirtying; not for purely local presentation.
## До изменения
Find sender, receiver, shared contract, authoritative mutation, serialization/conversion, dirtying, client application and owner test.
## Правила и границы
Messages express intent; server derives results. Replicate only data clients need and validate every client-originated request.
Use the current supported serialization and NetEntity conversion path for entity references; do not send raw server-only entities or broad component objects.
### Minimal examples

Correct: replicate minimal state and validate message intent on Server. Prohibited: send hidden data or client-claimed result. Stop: report unknown serialization/NetEntity contract.

## Выбор подхода
Prefer existing component state for ongoing visible state; use a small message for an action request or transient response.
After authoritative mutation, use the component or field dirtying pattern generated for that state. Trace sender, receiver and client state application before choosing a contract.
## Антипаттерны
- Trusting client values enables authority bugs.
- Replicating full components or secrets wastes bandwidth and leaks state.
- Forgetting dirtying leaves stale presentation.
- Treating a transient message as persistent state loses late-join and reconnect correctness.
## Проверка
Cover serialization, rejection, late/reconnect state and affected prediction.
## References
- [Networking and dirtying](references/networking-and-dirtying.md): Read this when adding or changing a replicated field, component state, dirtying behavior, network payload, or NetEntity conversion.
