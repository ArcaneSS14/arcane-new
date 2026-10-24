---
name: database-migrations
description: Change persistent models and SQLite/PostgreSQL migrations without breaking existing servers.
---

## Когда применять

Use for persistent models, SQLite/PostgreSQL schema, migrations, provider compatibility, backfills, or upgrade/rollback behavior.

## До изменения

Find the database owner, current provider conventions/package versions, model consumers, existing-row shape, nullability/default/index/unique/backfill needs, and rollback/data-loss contract.

## Правила и границы

- Persistence is server-only compatibility work; database entities are not gameplay components or Shared transport types.
- Generate/review matching SQLite and PostgreSQL migrations and snapshots. Fresh creation is insufficient: evaluate upgrade from existing schema and `Up`/`Down` separately.
- Consider existing rows, nullability, defaults, index/uniqueness, backfills, destructive change, rollback, atomicity, corruption, and provider-specific behavior. Report migrations not exercised against both providers.
- Confirm current EF/provider versions in `Directory.Packages.props`; do not assume old major behavior or build hand-made provider assumptions.

### Source ledger

Claim: Provider/version behavior must be read from current central package management and both SQLite/PostgreSQL migration conventions, not inferred from an older EF Core major.
Source: `Directory.Packages.props`, current database projects, and generated migration conventions.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: version-bound
Recheck trigger: Changing EF Core/provider package versions, target framework, or migration tooling.
- Stop if no supported persistence extension point exists: do not use reflection, alter engine, or create parallel persistence/test infrastructure.

### Minimal examples

Correct: review SQLite and PostgreSQL upgrade migrations separately. Prohibited: accept fresh database creation as upgrade proof. Stop: report unavailable provider migration validation.

## Выбор подхода

Prefer backward-compatible additive migration and explicit backfill/default. Use destructive migration only with explicit accepted data loss and a documented rollback/restore plan.

## Антипаттерны

- Do not treat a fresh database as upgrade proof, silently lose data, or block gameplay hot paths on database I/O.
- Do not invent provider command/API behavior.

## Проверка

When migration/model changes are made, inspect generated operations/snapshot and exercise existing upgrade coverage for each affected provider if available; report unavailable provider checks rather than claiming them.

## References

- [Migration workflow](references/migration-workflow.md): Read this when changing a persistent model, schema, relationship, index, or migration.
