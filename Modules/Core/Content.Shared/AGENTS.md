# Content.Shared project guidance

This guide describes the root project `Content.Shared/Content.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Trauma.Common`, `Content.Goobstation.Common`, `Content.Lavaland.Common`, `Content.Medical.Common`, `Content.Factory.Common`, `Content.Common`, `Content.Shared.Database`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Shared/Content.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Shared` (3890 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Trigger`](../../../Content.Shared/Trigger) | 193 | [`ActiveTimerTriggerComponent.cs`](../../../Content.Shared/Trigger/Components/ActiveTimerTriggerComponent.cs) |
| [`Weapons`](../../../Content.Shared/Weapons) | 136 | [`HitscanAmmoComponent.cs`](../../../Content.Shared/Weapons/Hitscan/Components/HitscanAmmoComponent.cs) |
| [`EntityEffects`](../../../Content.Shared/EntityEffects) | 118 | [`AddActionEntityEffectSystem.cs`](../../../Content.Shared/EntityEffects/Effects/AddActionEntityEffectSystem.cs) |
| [`Atmos`](../../../Content.Shared/Atmos) | 116 | [`AtmosCommandUtils.cs`](../../../Content.Shared/Atmos/AtmosCommandUtils.cs) |
| [`Speech`](../../../Content.Shared/Speech) | 85 | [`AccentEvent.cs`](../../../Content.Shared/Speech/AccentEvent.cs) |
| [`Movement`](../../../Content.Shared/Movement) | 78 | [`ActiveInputMoverComponent.cs`](../../../Content.Shared/Movement/Components/ActiveInputMoverComponent.cs) |
| [`Chemistry`](../../../Content.Shared/Chemistry) | 75 | [`DrainableSolutionComponent.cs`](../../../Content.Shared/Chemistry/Components/DrainableSolutionComponent.cs) |
| [`Damage`](../../../Content.Shared/Damage) | 69 | [`ActiveStaminaComponent.cs`](../../../Content.Shared/Damage/Components/ActiveStaminaComponent.cs) |
| [`Xenoarchaeology`](../../../Content.Shared/Xenoarchaeology) | 62 | [`XenoArtifactComponent.cs`](../../../Content.Shared/Xenoarchaeology/Artifact/Components/XenoArtifactComponent.cs) |
| [`Nutrition`](../../../Content.Shared/Nutrition) | 59 | [`InfantComponent.cs`](../../../Content.Shared/Nutrition/AnimalHusbandry/InfantComponent.cs) |
| [`Botany`](../../../Content.Shared/Botany) | 58 | [`ConsumeExudeGasGrowthComponent.cs`](../../../Content.Shared/Botany/Components/ConsumeExudeGasGrowthComponent.cs) |
| [`Shuttles`](../../../Content.Shared/Shuttles) | 58 | [`DockingInterfaceState.cs`](../../../Content.Shared/Shuttles/BUIStates/DockingInterfaceState.cs) |
| [`Silicons`](../../../Content.Shared/Silicons) | 57 | [`BorgSubtypePrototype.cs`](../../../Content.Shared/Silicons/Borgs/BorgSubtypePrototype.cs) |
| [`Construction`](../../../Content.Shared/Construction) | 54 | [`AnchorOnlyOnStationComponent.cs`](../../../Content.Shared/Construction/Components/AnchorOnlyOnStationComponent.cs) |
