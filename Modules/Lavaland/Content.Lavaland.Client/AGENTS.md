# Content.Lavaland.Client project guidance

This guide describes the root project `Content.Lavaland.Client/Content.Lavaland.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Lavaland.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Lavaland.Client/Content.Lavaland.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Lavaland.Client` (8 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Shuttles`](../../../Content.Lavaland.Client/Shuttles) | 3 | [`DockingConsoleSystem.cs`](../../../Content.Lavaland.Client/Shuttles/Systems/DockingConsoleSystem.cs) |
| [`(root)`](../../../Content.Lavaland.Client) | 1 | [`GlobalUsings.cs`](../../../Content.Lavaland.Client/GlobalUsings.cs) |
| [`Audio`](../../../Content.Lavaland.Client/Audio) | 1 | [`BossMusicSystem.cs`](../../../Content.Lavaland.Client/Audio/BossMusicSystem.cs) |
| [`Entry`](../../../Content.Lavaland.Client/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Lavaland.Client/Entry/EntryPoint.cs) |
| [`Pressure`](../../../Content.Lavaland.Client/Pressure) | 1 | [`PressureEfficiencyChangeSystem.cs`](../../../Content.Lavaland.Client/Pressure/PressureEfficiencyChangeSystem.cs) |
| [`Weapons`](../../../Content.Lavaland.Client/Weapons) | 1 | [`BlockChargeSystem.cs`](../../../Content.Lavaland.Client/Weapons/Block/BlockChargeSystem.cs) |

Configured output path: `../bin/Content.Client/`.
