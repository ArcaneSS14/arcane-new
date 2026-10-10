# Content.Server.Database project guidance

This guide describes the root project `Content.Server.Database/Content.Server.Database.csproj`; the project remains at the repository root.

## Ownership and contents

Server persistence models and database integration.

## Build and dependencies

Direct project references: `Content.Shared.Database`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Server.Database/Content.Server.Database.csproj --no-restore
```

## Source area map

This map is based on the existing C# files in `Content.Server.Database` (341 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Migrations`](../../../Content.Server.Database/Migrations) | 331 | [`20200929113117_Init.Designer.cs`](../../../Content.Server.Database/Migrations/Postgres/20200929113117_Init.Designer.cs) |
| [`(root)`](../../../Content.Server.Database) | 10 | [`DesignTimeContextFactories.cs`](../../../Content.Server.Database/DesignTimeContextFactories.cs) |

Configured output path: `../bin/Content.Server.Database/`.
