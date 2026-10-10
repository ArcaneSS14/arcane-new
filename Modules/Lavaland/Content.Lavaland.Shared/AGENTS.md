# Content.Lavaland.Shared project guidance

This guide describes the root project `Content.Lavaland.Shared/Content.Lavaland.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Lavaland.Shared/Content.Lavaland.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Lavaland.Shared` (139 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Megafauna`](../../../Content.Lavaland.Shared/Megafauna) | 39 | [`MegafaunaActiveBlinkComponent.cs`](../../../Content.Lavaland.Shared/Megafauna/Components/MegafaunaActiveBlinkComponent.cs) |
| [`Weapons`](../../../Content.Lavaland.Shared/Weapons) | 22 | [`BlockChargeComponent.cs`](../../../Content.Lavaland.Shared/Weapons/Block/BlockChargeComponent.cs) |
| [`EntityShapes`](../../../Content.Lavaland.Shared/EntityShapes) | 20 | [`AngerShapeSpawnerComponent.cs`](../../../Content.Lavaland.Shared/EntityShapes/Components/AngerShapeSpawnerComponent.cs) |
| [`Procedural`](../../../Content.Lavaland.Shared/Procedural) | 8 | [`LavalandMapComponent.cs`](../../../Content.Lavaland.Shared/Procedural/Components/LavalandMapComponent.cs) |
| [`Shuttles`](../../../Content.Lavaland.Shared/Shuttles) | 6 | [`DockingConsoleComponent.cs`](../../../Content.Lavaland.Shared/Shuttles/Components/DockingConsoleComponent.cs) |
| [`Anger`](../../../Content.Lavaland.Shared/Anger) | 5 | [`AdjustAngerOnHitComponent.cs`](../../../Content.Lavaland.Shared/Anger/Components/AdjustAngerOnHitComponent.cs) |
| [`Pressure`](../../../Content.Lavaland.Shared/Pressure) | 5 | [`PressureArmorChangeComponent.cs`](../../../Content.Lavaland.Shared/Pressure/PressureArmorChangeComponent.cs) |
| [`Aggression`](../../../Content.Lavaland.Shared/Aggression) | 4 | [`AggressiveComponent.cs`](../../../Content.Lavaland.Shared/Aggression/AggressiveComponent.cs) |
| [`Audio`](../../../Content.Lavaland.Shared/Audio) | 3 | [`BossMusicComponent.cs`](../../../Content.Lavaland.Shared/Audio/BossMusicComponent.cs) |
| [`Body`](../../../Content.Lavaland.Shared/Body) | 3 | [`CursedHeartComponent.cs`](../../../Content.Lavaland.Shared/Body/CursedHeartComponent.cs) |
| [`Damage`](../../../Content.Lavaland.Shared/Damage) | 3 | [`DamageSquareComponent.cs`](../../../Content.Lavaland.Shared/Damage/Components/DamageSquareComponent.cs) |
| [`Shelter`](../../../Content.Lavaland.Shared/Shelter) | 3 | [`SharedShelterCapsuleSystem.cs`](../../../Content.Lavaland.Shared/Shelter/SharedShelterCapsuleSystem.cs) |
| [`Chasm`](../../../Content.Lavaland.Shared/Chasm) | 2 | [`PreventChasmFallingComponent.cs`](../../../Content.Lavaland.Shared/Chasm/PreventChasmFallingComponent.cs) |
| [`MobPhases`](../../../Content.Lavaland.Shared/MobPhases) | 2 | [`MobPhasesComponent.cs`](../../../Content.Lavaland.Shared/MobPhases/MobPhasesComponent.cs) |
