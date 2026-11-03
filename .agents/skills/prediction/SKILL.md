---
name: prediction
description: Keep responsive shared interactions deterministic and free of duplicated side effects while server authority wins.
---
# prediction

## When to use

Use for responsive player actions executed on predicted paths; not for hidden, privileged, persistent or nondeterministic outcomes.

## Before editing

Trace input, predicted execution, server validation, authoritative mutation, reconciliation, presentation and test path.

## Rules and boundaries

Predicted code may execute repeatedly. Keep it deterministic and idempotent; server authority reconciles protected outcomes.
Separate deterministic state mutation from one-shot presentation. Reconciliation and rejection must be safe after latency, repeated input, deletion or observer state changes.

### Minimal examples

Correct: separate replayable state mutation from one-shot presentation. Prohibited: predict privileged irreversible result. Stop: report unknown reconciliation/authority owner.

## Choose an approach

Predict local correctable feedback; keep access, economy, random protected outcomes and persistence authoritative.
Use the established predicted feedback path when nearby code has one; do not pair an unconditional client-only effect with an unconditional authoritative effect.

## Avoid

- One-shot effects in both prediction and confirmation create double sound, popup, spawn or charge.
- Unsynchronized randomness desyncs client and server.
- Charging resources, spawning protected entities or committing state in both paths duplicates outcomes.

## Verification

Cover latency, rejection, repeated input, deletion, observers and reconciliation where affected.

## References

- [Predicted feedback](references/predicted-feedback.md): Read this when adding or reviewing a predicted popup, sound, spawn, effect, cooldown, UI update, or reconciliation path.
