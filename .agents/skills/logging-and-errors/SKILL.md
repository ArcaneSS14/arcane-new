---
name: logging-and-errors
description: Add useful diagnostics, choose log levels, preserve context, and handle player-facing failures safely.
---

# logging-and-errors

## Когда применять

Use when adding diagnostics, handling exceptions, classifying an operational failure, or producing safe player-facing feedback.

## До изменения

Identify the subsystem, operation, failure mode, expected rejection path, authority boundary, audience, and whether the code is a hot path. Establish what context is useful to an operator without exposing sensitive or untrusted data.

## Правила и границы

- Use debug/verbose for development detail, information for meaningful operational transitions, warning for recoverable unexpected state, and error for failed operations that require investigation. Do not log normal rejection as an error.
- Include subsystem, operation, entity/prototype identity, and safe context in structured or consistent searchable messages. Do not log credentials, tokens, private messages, or a full untrusted payload without necessity.
- Catch only to recover, clean up, translate to a domain result, or add useful context. Do not swallow exceptions; preserve stack information on rethrow.
- Keep operator diagnostics separate from localized player feedback. Player feedback must not expose stacks, file paths, SQL, or permission internals.
- Do not emit per-tick, per-entity broad-query, or expected-network-rejection spam. Use owner-local rate limiting or aggregation only when repeated diagnostics are actually needed.

### Minimal examples

Correct: log safe subsystem and operation context at the appropriate level. Prohibited: log credentials or normal rejection as error. Stop: report when safe diagnostic context cannot be identified.

## Выбор подхода

Log the smallest safe context that distinguishes the operation and owner. Return a localized domain failure to the player while keeping diagnostics with operators. Add aggregation only after proving a repeated operational signal, not to conceal an unbounded hot path.

## Антипаттерны

- Do not classify a normal invalid request as an error, log secrets, or concatenate uncontrolled payloads into diagnostics.
- Do not catch-and-ignore, rethrow without stack preservation, or expose internal diagnostic text to players.
- Do not log a normal loop iteration merely because the code lacks another signal.

## Проверка

Exercise success, expected rejection, malformed input, and exception paths. Confirm the message has enough safe context, no secrets/private payload, correct operational level, and no hot-path spam.

## References

No reference is required for ordinary diagnostics; inspect the owner subsystem's existing logging conventions before adding a new message.
