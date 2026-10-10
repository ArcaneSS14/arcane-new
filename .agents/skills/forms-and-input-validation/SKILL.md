---
name: forms-and-input-validation
description: Validate UI, command, text, numeric, entity, and configuration input with localized feedback and server authority.
---

Validation has two layers: client feedback and authoritative server enforcement.

## Parsing

Trim deliberately. Parse numbers with an explicit culture and format policy. Reject ambiguous forms such as integer fields supplied as `1.0` unless the domain permits them. Check length before expensive processing.

## Domain validation

Validate ranges, overflow, whitelists, prototype IDs, entity existence, ownership, access, cooldowns, and cross-field constraints. Normalize case and whitespace only when the domain defines them as insignificant.

Use one authoritative validation source where possible. Do not allow UI and server rules to drift.

## Feedback

Return specific player-safe errors in `en-US`; add `ru-RU` only when explicitly requested. Do not expose hidden state, internal exceptions, raw IDs, or server-only reasons.

Variable names and values passed into validation messages must match `en-US`; match Russian too only when Russian is in scope.

## Security failures

Treat rich text, markup, paths, URLs, and command fragments as hostile. Use structured APIs instead of concatenating shell, SQL, markup, or resource paths.

## Verification commands

Build only the affected project. When verification is requested, choose focused tests for the changed validation boundary (such as BUI, command authority, stale entities, or client/server behavior). Do not restore dependencies or run complete test suites by default. Relevant cases include empty and whitespace input, boundaries, overflow, locale variants when in scope, malformed markup, unauthorized actors, and conflicting fields.
 Test empty, whitespace, boundaries, overflow, locale variants, malformed markup, unauthorized actors, and conflicting fields.
