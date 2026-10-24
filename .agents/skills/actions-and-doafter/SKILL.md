---
name: actions-and-doafter
description: Implement actions, cooldowns, DoAfter flows, interruption, cancellation, and authoritative completion safely.
---

# Granted actions and timed completion

Trace grant source → action identity/state → UI availability → request → validation → optional DoAfter → completion/cooldown → revoke. The source may be equipment, component, prototype, role or runtime state; grant/revoke must be idempotent and follow that source's lifetime, including body/source transfer when affected.

Use the current action/DoAfter event pattern. Share only contracts needed by clients/prediction. The server checks actor, target, range, access, current state and cooldown at the authoritative point. Prevent duplicate starts/completions where they would spend or grant twice.

Define relevant interruption conditions (movement, damage, hand/target change, incapacitation or deletion). Revalidate on completion: start-time checks do not guarantee the world is unchanged. Charge costs/cooldowns once at the intended point; an early valuable mutation needs explicit rollback. Cancellation must leave consistent state and feedback.

Predicted animation/audio/popups must not repeat on confirmation; see [prediction](../prediction/SKILL.md). Select checks for changed transitions under [verification](../../rules/verification.md), including configured cancellation or latency only where they affect the contract.
