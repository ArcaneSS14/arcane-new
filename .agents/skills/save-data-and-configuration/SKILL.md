---
name: save-data-and-configuration
description: Validate, migrate, and atomically persist files and configuration with safe fallback and restart coverage.
---

Persistent files and configuration are compatibility and reliability surfaces.

## Reading

Treat files as untrusted input. Validate schema, bounds, enums, paths, identifiers, required fields, and cross-field rules. Define behavior for missing, empty, truncated, malformed, and future-version data.

## Defaults and migration

Defaults must satisfy whitelists and cross-field constraints. Normalize legacy values before exposing them to gameplay. Back up unreadable legacy data before reset when recovery matters.

Version formats when changes are not backward-compatible. Do not silently accept impossible or dangerous configuration.

## Writing

Write to a unique temporary file, flush when durability matters, and replace atomically. Clean temporary files on every failure path. Never overwrite a valid file with partial or unvalidated output.

## Player-facing settings

Names, descriptions, options, validation errors, and fallback notices require `en-US`; update `ru-RU` only when explicitly requested. Culture names and saved culture values must be validated against cultures actually found at runtime.

A saved setting must restore after restart. Removed or invalid values must fall back safely without corrupting the config.

## Verification commands

Build only the affected project. When verification is requested, select focused tests for persistence, reload, client settings, propagation, or restart behavior. Do not restore dependencies or run complete test suites by default. Relevant cases include missing, malformed, legacy, partial, concurrent, out-of-range, backup, normalization, and restart scenarios.
 Test missing, malformed, legacy, partial, concurrent, out-of-range, backup, normalization, and restart cases.
