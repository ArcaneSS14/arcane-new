---
name: security-and-validation
description: Review trust boundaries, input validation, permissions, abuse resistance, secrets, and safe failure behavior.
---

# security-and-validation

## Когда применять

Use for client, command, file, network, external-service, permission, secret, or abuse-resistance boundaries.

## До изменения

Identify untrusted input, authoritative owner, protected result, target/permission/state/range contract, failure path, rate/replay risk, persistence/path handling, and existing owner validation/test path.

## Правила и границы

- Treat input as untrusted until server/authoritative owner validates identity, ownership, permissions, target/range/state, quantity/bounds, resource availability, rate limits, and replay behavior. Recompute protected values; never trust client displays/state.
- Reject malformed/unauthorized requests before partial mutation. Use safe domain failure for expected rejection and avoid hidden-state disclosure through distinct errors.
- Never commit/log credentials, tokens, keys, webhook secrets, or production connection strings. Use repository secrets/environment configuration.
- Normalize/constrain user paths; prevent traversal, arbitrary overwrite, shell interpolation, and unsafe temporary-file behavior. Use atomic replacement/explicit permissions where persistence needs them.
- Stop at a missing supported boundary: do not bypass access, use reflection, change engine, or create parallel authorization/validation infrastructure.

### Minimal examples

Correct: recompute protected input/result at authoritative owner. Prohibited: use a hidden UI control as security. Stop: report missing server validation extension point.

## Выбор подхода

Place validation at the authoritative owner and reuse its public API. Add owner-local rate limiting for a resource-intensive authoritative handler when its threat model identifies a client/network entry point, protected resource, expected cost, limiter scope, and effect on legitimate traffic; reproduced abuse is evidence, not a prerequisite. Do not add a limiter to every public API. Separate safe player feedback from operator diagnostics.

## Антипаттерны

- Do not rely on client-side checks, swallow partial failure, leak protected state, or turn normal invalid input into an exception path.
- Do not treat obfuscation, `CONFIDENTIAL`, or a hidden UI control as security.

## Проверка

When the boundary changes, cover unauthorized access, malformed/boundary payload, replay/concurrent request, stale target, partial failure, and absence of secrets in output with existing owner tests.

## References

No reference is required for ordinary validation routing; inspect the current authoritative owner before adding a security check.
