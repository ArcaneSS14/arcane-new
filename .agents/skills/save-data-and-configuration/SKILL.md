---
name: save-data-and-configuration
description: Validate, migrate, and atomically persist files and configuration with safe fallback and restart coverage.
---

# File-backed state and settings

Identify the existing persistence owner/API first. CVars and relational databases keep their own writers; do not wrap them in an unrelated temp/rename implementation. File formats, defaults and restart behavior are compatibility contracts.

Treat loaded data as untrusted. Validate schema, enum/ID/path/bounds and cross-field rules; define missing, empty, truncated, malformed, legacy and future-version behavior. Defaults must satisfy the same constraints. Normalize accepted legacy data before gameplay; version incompatible formats and preserve unreadable originals when recovery policy needs them.

For an owned custom writer read [durable-write](references/durable-write.md). Do not overwrite valid state with partial/unvalidated output. Define concurrent-writer and recovery behavior through that owner's supported mechanism.

Saved settings must restore after restart and fall back safely for removed/invalid values. Validate culture values against actually loaded cultures; labels/errors and view culture follow [localization-in-code](../localization-in-code/SKILL.md).

Select affected load/migration/reload/propagation/restart/failure checks under [verification](../../rules/verification.md). Not every settings label needs the whole persistence matrix.
