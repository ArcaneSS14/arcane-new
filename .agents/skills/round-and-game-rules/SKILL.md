---
name: round-and-game-rules
description: Implement round lifecycle, game rules, objectives, announcements, win conditions, and restart-safe cleanup.
---

## Когда применять

Use for round lifecycle, game rules, objectives, announcements, win conditions, player assignment, or restart cleanup.

## До изменения

Trace activation, round start, assignment, updates, end conditions, round end/restart, rule entity/state, prototype/serialized contract, presentation, and cleanup owner.

## Правила и границы

- Keep authoritative rule state on Server; replicate only needed presentation. Prefer rule entities and existing game-rule APIs over static state.
- Handle late join, reconnect, ghosting, body/mind transfer, disconnect, administrative intervention, no eligible players, and duplicate role/objective assignment.
- Compute win conditions from current authoritative state and restrict hidden antagonist/objective information to intended recipients. Localize announcements/results.
- Clean spawned entities, timers, subscriptions, overlays, temporary CVars, and round caches at end/restart so no state leaks to the next round.
- Stop at absent owner extension point: no reflection, engine edits, client-authoritative rule outcomes, or parallel round framework.

### Minimal examples

Correct: clean round-scoped timers/entities on round end. Prohibited: retain global static rule state across restart. Stop: report missing game-rule lifecycle owner.

## Выбор подхода

Extend the established rule/entity/objective owner and lifecycle events; keep rule-specific state serialized/prototype-backed only when it belongs to that contract.

## Антипаттерны

- Do not assume system/entity lives one round, expose hidden result state, or leave cleanup to a detached timer.

## Проверка

When changed, cover activation, late/no eligible players, disconnect/deletion, simultaneous rules, early/normal end, and restart via targeted owner tests.

## References

- [Round lifecycle](references/round-lifecycle.md): Read this when changing game-rule selection, activation, player assignment, end conditions, cleanup, or restart behavior.
