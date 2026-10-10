# Content.Tools project guidance

This guide describes the root project `Content.Tools/Content.Tools.csproj`; the project remains at the repository root.

## Ownership and contents

Repository developer tooling; keep changes scoped to the specific command/tool behavior.

## Build and dependencies

Direct project references: No direct project references. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Tools/Content.Tools.csproj --no-restore
```

## Source area map

This map is based on the existing C# files in `Content.Tools` (5 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Tools) | 5 | [`Map.cs`](../../../Content.Tools/Map.cs) |
