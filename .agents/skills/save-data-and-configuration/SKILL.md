---
name: save-data-and-configuration
description: Validate, migrate, and atomically persist files and configuration with safe fallback and restart coverage.
---

# save-data-and-configuration

## Когда применять

Use for save data, configuration schema/versioning, migration, atomic write, settings recovery, or restart behavior.

## До изменения

Find the persistence owner, schema/version, consumers, compatibility/rollback contract, file lifecycle, atomic-write owner, and existing targeted persistence/restart test path.

## Правила и границы

- Treat files as untrusted: validate schema, bounds, enums, paths, IDs, required/cross-field rules, missing/empty/truncated/malformed/future-version data.
- Defaults satisfy whitelist/cross-field constraints; normalize legacy values before gameplay. Version incompatible formats and back up unreadable legacy data before reset when recovery matters.
- Write a unique temporary file, flush when durability matters, replace atomically, and clean temporary files on every failure. Never overwrite valid data with partial/unvalidated output.
- Player-visible setting text/fallbacks use `en-US`; Russian only on request. Validate saved culture names against available runtime cultures and restore/fallback invalid/removed values safely.
- Stop without a supported persistence API: do not reflect, alter engine, bypass atomic owner behavior, or create parallel persistence infrastructure.

### Minimal examples

Correct: validate then atomically replace persisted configuration. Prohibited: overwrite valid data with partial output. Stop: report unknown schema/migration/recovery behavior.

## Выбор подхода

Use backward-compatible migration and safe fallback before destructive reset; scope persistence to its owner and define restart/rollback/corruption behavior explicitly.

## Антипаттерны

- Do not silently accept impossible/dangerous config, overwrite a valid file, or treat a successful write as restart proof.

## Проверка

When persistence changes, select existing owner coverage for missing/malformed/legacy/partial/concurrent/out-of-range/backup/normalization/restart paths. Do not restore or run full suites by default.

## References

- [Durable write](references/durable-write.md): Read this when implementing or reviewing configuration or save-data writes to disk.
