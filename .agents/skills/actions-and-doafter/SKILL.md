---
name: actions-and-doafter
description: Implement actions, cooldowns, DoAfter flows, interruption, cancellation, and authoritative completion safely.
---
# actions-and-doafter

## Когда применять

Use actions for reusable abilities and DoAfter for interruptible timed completion.

## До изменения

Find the source (prototype, component startup, equipment, role, or runtime), owner system, Shared identity/event contract when prediction or UI needs it, target, and grant/transfer/removal lifecycle.

## Правила и границы

Validate user, target, range, access, cooldown, and current state on the server. Start exactly one action/DoAfter; apply resources, cooldowns, and persistent effects at the correct authoritative point; revalidate on completion and remove or disable the action when its source disappears.

### Minimal examples

Correct: start one owner DoAfter and revalidate on completion. Prohibited: commit a protected outcome before an interruptible delay. Stop: report when the current action/DoAfter owner pattern cannot be confirmed.

## Выбор подхода

Define interruption for movement, damage, hand change, target movement/change, target deletion, range loss, incapacitation, and duplicate requests. Do not commit valuable state before completion without explicit rollback. Predict only correctable animation, popup, or sound and prevent duplicate feedback on server confirm/reject.

## Антипаттерны

Do not commit protected outcome before completion or duplicate predicted/server feedback.

## Проверка

Cover success, every interrupt, deletion, disconnect, cooldown, source removal and latency rejection.

## References

- [Action lifecycle](references/action-lifecycle.md): Read this when adding, transferring, removing, or debugging an action across entity, body, or round lifecycle changes.
- [DoAfter checklist](references/doafter-checklist.md): Read this when adding or changing a DoAfter, its interruption rules, or its completion handler.
