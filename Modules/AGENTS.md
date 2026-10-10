<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Module guidance

Use [`CATALOG.md`](CATALOG.md) to map a `Content.*` project to its owner guidance and understand its project references and build impact. The owner guides under `Modules/` document root-level runtime projects; they do not move those projects into this directory.

`Modules/` currently contains documentation directories; runtime projects and resources remain in root project/resource paths. No `module.yml` manifests are present in this tree. Use the linked project guidance and actual `.csproj` references to establish ownership; if a future runtime module provides a `module.yml`, read it before editing that module.

Keep module code in its existing Common, Shared, Server, and Client projects. Keep resources in the established root `Resources` owner paths. Verify project references before introducing a dependency.

Do not use one module as a dumping ground for another module's feature. If base content must change to expose an extension point, keep that change minimal and leave the feature implementation in its owning module.
