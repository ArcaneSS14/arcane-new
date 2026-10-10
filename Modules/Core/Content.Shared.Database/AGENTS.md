# Content.Shared.Database project guidance

This guide describes the root project `Content.Shared.Database/Content.Shared.Database.csproj`; the project remains at the repository root.

## Ownership and contents

Database contracts shared between server-side persistence consumers.

## Build and dependencies

Direct project references: No direct project references. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Shared.Database/Content.Shared.Database.csproj --no-restore
```

## Source area map

This map is based on the existing C# files in `Content.Shared.Database` (6 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Shared.Database) | 6 | [`Bans.cs`](../../../Content.Shared.Database/Bans.cs) |
