---
name: external-services
description: Integrate HTTP APIs, webhooks, external processes, retries, timeouts, caching, and secret-backed configuration safely.
---

# external-services

## When to use

Use for HTTP APIs, webhooks, external processes, retries, timeouts, caches, or secret-backed configuration.

## Before editing

Identify service owner, configuration/secret source, request/response schema, timeout/cancellation owner, idempotency, retry policy, stale-data behavior, simulation-thread boundary, and failure reporting.

## Rules and boundaries

- Treat external systems/payloads as unreliable and untrusted. Validate URL/configuration at startup, response status/schema/content type/size before use, and do not deserialize arbitrary polymorphic payloads.
- Set explicit timeout and cancellation. Retry only idempotent operations with bounded backoff; prevent retry storms and define cache/stale fallback.
- Do not block the simulation thread on network I/O. Marshal results by the supported owner path and revalidate entity/round state before mutation.
- Use secret mechanisms, CVars, or environment configuration as established; never commit secrets/endpoints containing secrets, send unnecessary private data, or log secrets/payloads.
- Keep request handlers task-observable: do not use `async void` except a framework-required event handler, and do not synchronously wait on `Task` from simulation/UI flow. A cancellation, timeout, or exception must leave no partial authoritative mutation or unreleased request state.
- Stop without a supported extension point: no reflection, engine edits, or parallel service/retry framework.

### Minimal examples

Correct: apply a validated remote result through the supported owner path. Prohibited: retry a non-idempotent mutation. Stop: report an unavailable service/configuration contract.

## Choose an approach

Use an existing service client/configuration owner, bounded cache for stable data, and clear unavailable-service fallback. Prefer failure reporting with safe operator context over user-visible raw remote errors.

## Avoid

- Do not retry non-idempotent mutations, retain stale callback results, or trust remote input as authority.
- Do not hide timeout/cancellation/outage as success.

## Verification

When the integration changes, check timeout, cancellation, invalid endpoint/certificate, non-success/malformed/oversized response, duplicate callback, stale result, and outage with targeted owner coverage.

## References

- [Service failure matrix](references/service-failure-matrix.md): Read this when adding or changing an HTTP, webhook, external-process, or other remote-service integration.
