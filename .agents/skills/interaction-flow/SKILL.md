---
name: interaction-flow
description: Implement reusable interactions, verbs, in-hand use, actions, and authoritative outcomes.
---
# interaction-flow

## Когда применять

Use for player action/verb/input flows.

## До изменения

Trace the entry point (verb, action, in-hand use, BUI, or event), state owner, server handler, mutation, feedback, and nearest owner-local or integration test. Establish the owner of `handled`/`cancelled` and whether the flow is predicted.

## Правила и границы

Route `On...` to `Try...` to `Can...` to one execution path. `Can...` never mutates state, spends resources, or spawns entities, and must support quiet probes without feedback spam. `Try...` repeats its own checks even after verb or UI feedback; the server revalidates actor, target, and current state before protected mutation.

### Minimal examples

Correct: route On to Try to pure Can to one mutation path. Prohibited: mutate in Can. Stop: report unknown handler/cancellation contract.

## Выбор подхода

Use handled/cancelled flags only with an explicit owner and contract. Perform resources, cooldowns, and persistent mutation through the one owner path after successful validation; presentation is not evidence of success.

## Антипаттерны

Do not trust client-claimed success, split valuable or persistent mutation across callers, or turn `Can...` into a mutation path for convenience.

## Проверка

Cover rejection, repeated invocation, stale targets, and valuable or persistent changes; for prediction, ensure accept/reject cannot duplicate feedback.

## References

No reference is required for ordinary interaction routing.
