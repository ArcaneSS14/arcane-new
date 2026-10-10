# Content.Goobstation.Common project guidance

This guide describes the root project `Content.Goobstation.Common/Content.Goobstation.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: `Content.Common`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Goobstation.Common/Content.Goobstation.Common.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Goobstation.Common` (88 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Speech`](../../../Content.Goobstation.Common/Speech) | 9 | [`BoganAccentComponent.cs`](../../../Content.Goobstation.Common/Speech/BoganAccentComponent.cs) |
| [`Weapons`](../../../Content.Goobstation.Common/Weapons) | 8 | [`DelayedKnockdownComponent.cs`](../../../Content.Goobstation.Common/Weapons/DelayedKnockdown/DelayedKnockdownComponent.cs) |
| [`Barks`](../../../Content.Goobstation.Common/Barks) | 3 | [`BarkEvents.cs`](../../../Content.Goobstation.Common/Barks/BarkEvents.cs) |
| [`Body`](../../../Content.Goobstation.Common/Body) | 3 | [`BodyEvents.cs`](../../../Content.Goobstation.Common/Body/BodyEvents.cs) |
| [`StationReport`](../../../Content.Goobstation.Common/StationReport) | 3 | [`CommonNtrStationReportSystem.cs`](../../../Content.Goobstation.Common/StationReport/CommonNtrStationReportSystem.cs) |
| [`Stunnable`](../../../Content.Goobstation.Common/Stunnable) | 3 | [`GetClothingStunModifierEvent.cs`](../../../Content.Goobstation.Common/Stunnable/GetClothingStunModifierEvent.cs) |
| [`BlockTeleport`](../../../Content.Goobstation.Common/BlockTeleport) | 2 | [`BlockTeleportComponent.cs`](../../../Content.Goobstation.Common/BlockTeleport/BlockTeleportComponent.cs) |
| [`Chemistry`](../../../Content.Goobstation.Common/Chemistry) | 2 | [`ChemistryEvents.cs`](../../../Content.Goobstation.Common/Chemistry/ChemistryEvents.cs) |
| [`Clothing`](../../../Content.Goobstation.Common/Clothing) | 2 | [`CheckClothingSlotHiddenEvent.cs`](../../../Content.Goobstation.Common/Clothing/CheckClothingSlotHiddenEvent.cs) |
| [`Flammability`](../../../Content.Goobstation.Common/Flammability) | 2 | [`FireImmunityComponent.cs`](../../../Content.Goobstation.Common/Flammability/FireImmunityComponent.cs) |
| [`Movement`](../../../Content.Goobstation.Common/Movement) | 2 | [`MoverControllerEvents.cs`](../../../Content.Goobstation.Common/Movement/MoverControllerEvents.cs) |
| [`Pirates`](../../../Content.Goobstation.Common/Pirates) | 2 | [`RansomComponent.cs`](../../../Content.Goobstation.Common/Pirates/RansomComponent.cs) |
| [`ServerCurrency`](../../../Content.Goobstation.Common/ServerCurrency) | 2 | [`ICommonCurrencyManager.cs`](../../../Content.Goobstation.Common/ServerCurrency/ICommonCurrencyManager.cs) |
| [`Silicons`](../../../Content.Goobstation.Common/Silicons) | 2 | [`ActiveExperimentalLawProviderComponent.cs`](../../../Content.Goobstation.Common/Silicons/Components/ActiveExperimentalLawProviderComponent.cs) |
