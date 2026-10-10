# Content.MapRenderer project guidance

This guide describes the root project `Content.MapRenderer/Content.MapRenderer.csproj`; the project remains at the repository root.

## Ownership and contents

Map rendering utility that consumes integration-test/content infrastructure; keep changes scoped to rendering and its input pipeline.

## Build and dependencies

Direct project references: `Content.IntegrationTests`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.MapRenderer/Content.MapRenderer.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.MapRenderer` (15 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Painters`](../../../Content.MapRenderer/Painters) | 7 | [`DecalData.cs`](../../../Content.MapRenderer/Painters/DecalData.cs) |
| [`(root)`](../../../Content.MapRenderer) | 6 | [`CommandLineArguments.cs`](../../../Content.MapRenderer/CommandLineArguments.cs) |
| [`Extensions`](../../../Content.MapRenderer/Extensions) | 2 | [`DirectoryExtensions.cs`](../../../Content.MapRenderer/Extensions/DirectoryExtensions.cs) |

Configured output path: `../bin/Content.MapRenderer/`.
