# Content.Lavaland.Server project guidance

This guide describes the root project `Content.Lavaland.Server/Content.Lavaland.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Lavaland.Shared`, `Content.Server`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Lavaland.Server/Content.Lavaland.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Lavaland.Server` (28 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Procedural`](../../../Content.Lavaland.Server/Procedural) | 6 | [`LavalandGridGrantComponent.cs`](../../../Content.Lavaland.Server/Procedural/Components/LavalandGridGrantComponent.cs) |
| [`Tendril`](../../../Content.Lavaland.Server/Tendril) | 4 | [`TendrilComponent.cs`](../../../Content.Lavaland.Server/Tendril/Components/TendrilComponent.cs) |
| [`Biome`](../../../Content.Lavaland.Server/Biome) | 2 | [`BiomeOptimizeComponent.cs`](../../../Content.Lavaland.Server/Biome/BiomeOptimizeComponent.cs) |
| [`Commands`](../../../Content.Lavaland.Server/Commands) | 2 | [`LavalandListingCommand.cs`](../../../Content.Lavaland.Server/Commands/LavalandListingCommand.cs) |
| [`Mobs`](../../../Content.Lavaland.Server/Mobs) | 2 | [`SpawnLootOnDeathComponent.cs`](../../../Content.Lavaland.Server/Mobs/SpawnLootOnDeathComponent.cs) |
| [`Shuttles`](../../../Content.Lavaland.Server/Shuttles) | 2 | [`DockingConsoleSystem.cs`](../../../Content.Lavaland.Server/Shuttles/Systems/DockingConsoleSystem.cs) |
| [`Weapons`](../../../Content.Lavaland.Server/Weapons) | 2 | [`BlockChargeSystem.cs`](../../../Content.Lavaland.Server/Weapons/Block/BlockChargeSystem.cs) |
| [`(root)`](../../../Content.Lavaland.Server) | 1 | [`GlobalUsings.cs`](../../../Content.Lavaland.Server/GlobalUsings.cs) |
| [`Audio`](../../../Content.Lavaland.Server/Audio) | 1 | [`BossMusicSystem.cs`](../../../Content.Lavaland.Server/Audio/BossMusicSystem.cs) |
| [`EntityConditions`](../../../Content.Lavaland.Server/EntityConditions) | 1 | [`PressureConditionSystem.cs`](../../../Content.Lavaland.Server/EntityConditions/PressureConditionSystem.cs) |
| [`Entry`](../../../Content.Lavaland.Server/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Lavaland.Server/Entry/EntryPoint.cs) |
| [`Megafauna`](../../../Content.Lavaland.Server/Megafauna) | 1 | [`MegafaunaRejuvenateSystem.cs`](../../../Content.Lavaland.Server/Megafauna/Systems/MegafaunaRejuvenateSystem.cs) |
| [`Pressure`](../../../Content.Lavaland.Server/Pressure) | 1 | [`PressureEfficiencyChangeSystem.cs`](../../../Content.Lavaland.Server/Pressure/PressureEfficiencyChangeSystem.cs) |
| [`Salvage`](../../../Content.Lavaland.Server/Salvage) | 1 | [`ShelterCapsuleSystem.cs`](../../../Content.Lavaland.Server/Salvage/ShelterCapsuleSystem.cs) |

Configured output path: `../bin/Content.Server/`.
