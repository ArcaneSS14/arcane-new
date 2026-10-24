---
name: database-migrations
description: Change persistent models and SQLite/PostgreSQL migrations without breaking existing servers.
---

# Upgrade persistent models

Find the owning database model and existing SQLite/PostgreSQL migration conventions. Keep persistence server-side; database entities are not gameplay components or transport contracts. Keep I/O off frequent gameplay paths.

For a schema change review existing rows, nullability, defaults/backfill, indexes, uniqueness and relationships. Generate both providers' migrations and inspect Up/Down operations and snapshots separately. A fresh database does not prove an existing database can upgrade. Define failure/rollback expectations and preserve data unless destructive change is explicitly accepted.

Record model change → SQLite output → PostgreSQL output → existing-data treatment → upgrade evidence/omissions. Search the changed operation's provider differences rather than assuming equivalent SQL. Select existing upgrade checks under [verification](../../rules/verification.md) and report a provider not exercised.

Directory.Packages.props owns current EF Core, SQLite and Npgsql versions. Consult version-specific provider guidance only for the operation being changed (e.g. date/time representation, query translation, generated column names or tool targeting). A major-version list is not a timeless guarantee or a mandatory tutorial for every migration.
