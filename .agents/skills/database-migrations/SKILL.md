---
name: database-migrations
description: Change persistent models and SQLite/PostgreSQL migrations without breaking existing servers.
---

Persistence is server-only compatibility work.

## Workflow

1. Find the owning database project and both provider conventions.
2. Update the model deliberately.
3. Consider existing rows, nullability, defaults, indexes, uniqueness, and backfills.
4. Generate matching SQLite and PostgreSQL migrations.
5. Review generated operations and snapshots.
6. Test upgrading an existing database, not only creating a new one.
7. Keep database I/O off frequent gameplay paths.

Do not use database entities as gameplay components or shared transport types. Avoid destructive schema changes unless data loss is explicitly accepted.

Report when migrations were generated but not exercised against both providers.

## Provider versions in effect

Central package versions live in `Directory.Packages.props`; do not assume an older major version from prior work. Both providers are in use, and their majors change behavior independently.

- EF Core and `Microsoft.EntityFrameworkCore.Sqlite.Core` are on 10.x, `Npgsql.EntityFrameworkCore.PostgreSQL` on its own matching major. Confirm the current numbers before writing provider-specific code.
- EF Core 10 changes several defaults that can silently alter a migration: parameterized collections now use multiple scalar parameters instead of a JSON array, complex-type column names are uniquified, nested complex properties use a full path in the column name, and SQL parameter names are simplified.
- EF Core 10 also changes `ExecuteUpdateAsync` to accept a regular lambda, which breaks hand-built expression trees.
- Microsoft.Data.Sqlite 10 assumes UTC: `GetDateTimeOffset` on a value without an offset treats it as UTC, `GetDateTime` with an offset returns UTC, and writing a `DateTimeOffset` into a `REAL` column converts to UTC first.
- EF tools now require `--framework` for multi-targeted projects.

Because existing servers keep their old schema, a migration that is correct for a fresh database can still fail on upgrade. Review the generated `Up`/`Down` operations and the snapshot, and confirm SQLite and PostgreSQL outputs separately.
