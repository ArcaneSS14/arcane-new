# Content.Replay project guidance

This guide describes the root project `Content.Replay/Content.Replay.csproj`; the project remains at the repository root.

## Ownership and contents

Replay executable and replay-related content integration.

## Build and dependencies

Direct project references: `Content.Shared`, `Content.Client`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Replay/Content.Replay.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Replay` (6 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Replay) | 3 | [`GlobalUsings.cs`](../../../Content.Replay/GlobalUsings.cs) |
| [`Menu`](../../../Content.Replay/Menu) | 3 | [`ReplayMainMenu.cs`](../../../Content.Replay/Menu/ReplayMainMenu.cs) |

Configured output path: `../bin/Content.Replay/`.
