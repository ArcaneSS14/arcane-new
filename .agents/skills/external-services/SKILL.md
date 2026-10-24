---
name: external-services
description: Integrate HTTP APIs, webhooks, external processes, retries, timeouts, caching, and secret-backed configuration safely.
---

External systems are unreliable and untrusted. Keep gameplay resilient when they are slow or unavailable.

## Configuration and secrets

Use CVars for non-sensitive operational settings only; deliver credentials through the supported environment/repository secret mechanism. CONFIDENTIAL is not secret storage; see [commands-and-cvars](../commands-and-cvars/SKILL.md). Never commit credentials or production endpoints that contain secrets. Validate URLs and configuration at startup.

## Requests

Set explicit timeouts, cancellation, bounded response sizes, and supported content types. Validate status codes and response schemas before use. Do not deserialize arbitrary polymorphic payloads.

## Reliability

Distinguish a request known not to have started from a lost response after a possible external commit. Retry only with a proved idempotent/keyed operation or reconciliation that prevents duplicate effects. Use bounded backoff and avoid retry storms. Cache stable data where appropriate and define stale-data behavior.

## Simulation boundary

Do not block the main simulation thread on network I/O. Apply results through the supported main-thread path and revalidate entity or round state before mutation.

## Privacy and logging

Send only required data. Avoid player-identifying or private content unless the feature explicitly requires and documents it. Redact secrets and payloads from logs.

## Verification

Select only cases exercising the changed contract under [verification](../../rules/verification.md); the following list is conditional, not a required full suite.

Test timeout, cancellation, invalid certificate or endpoint, non-success status, malformed response, oversized response, duplicate callback, stale result, and service outage.

For changed failure handling consider connection/auth/rate-limit failure, malformed or duplicate responses, round-long outage and shutdown while pending. These are conditional scenarios; preserve predictable failure without blocking simulation or applying stale results.
