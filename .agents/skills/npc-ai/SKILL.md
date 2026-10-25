---
name: npc-ai
description: Modify NPC behavior, HTN tasks, navigation, steering, prototypes, and AI debugging.
---

# npc-ai

## Когда применять

Use for server-owned NPC target selection, HTN tasks/operators, utility selection, navigation, steering, cooldowns, prototype configuration, and AI debugging.

## До изменения

Trace the server owner, task/HTN/utility selection, target acquisition, pathfinding/steering, cooldown/cancellation, prototype configuration, and debug/test path. Identify where selection ends, planning begins, execution mutates state, and cleanup invalidates an old plan.

## Правила и границы

- Keep protected AI decisions server-authoritative; a client never chooses protected targets, plans, or outcomes.
- Separate target selection, planning, execution, and cleanup/cancellation. Use established navigation and steering APIs rather than starting a parallel AI loop.
- Cancel or invalidate plan/navigation on entity deletion, unreachable target, target/goal change, map transfer, or component removal.
- Bound query scope and replan frequency. Do not perform an expensive global search every update when the existing owner has a bounded query, cooldown, or event-driven refresh.
- Keep observable configuration prototype-backed where the owner expects it, and retain existing debug/test paths for behavior investigation.

### Minimal examples

Correct: invalidate navigation after target deletion or map transfer. Prohibited: run a global target search each update. Stop: report missing task/navigation owner API.

## Выбор подхода

Use the current task/HTN/utility path for the behavior family being changed. Reuse existing pathfinding/steering and cancellation ownership; add a new plan only when no existing owner contract covers the objective. Prefer an owner-local cooldown or state change over repeated polling.

## Антипаттерны

- Do not create a second navigation/AI update loop, leave navigation active after its target disappears, or retain a plan across incompatible map transfer.
- Do not let a client provide a protected AI result or use an unbounded global target search every update.
- Do not merge selection, execution, and cleanup until stale plans cannot be invalidated independently.

## Проверка

Check no target, unreachable target, repeated planning, target deletion, goal change, map transfer, cancellation, and observable prototype behavior. Use existing owner-local or integration coverage when the changed behavior has a test path.

## References

No reference is required for ordinary NPC routing; inspect the closest existing task, navigation, or steering owner before adding behavior.
