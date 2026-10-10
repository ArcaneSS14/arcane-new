# Content.Goobstation.Shared project guidance

This guide describes the root project `Content.Goobstation.Shared/Content.Goobstation.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Shared`, `Content.Medical.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Goobstation.Shared/Content.Goobstation.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Goobstation.Shared` (836 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Wraith`](../../../Content.Goobstation.Shared/Wraith) | 124 | [`ActionUseDelayOnUseComponent.cs`](../../../Content.Goobstation.Shared/Wraith/Actions/ActionUseDelayOnUseComponent.cs) |
| [`Shadowling`](../../../Content.Goobstation.Shared/Shadowling) | 48 | [`ShadowlingAnnihilateComponent.cs`](../../../Content.Goobstation.Shared/Shadowling/Components/Abilities/Ascension/ShadowlingAnnihilateComponent.cs) |
| [`Enchanting`](../../../Content.Goobstation.Shared/Enchanting) | 32 | [`BonusDamageEnchantComponent.cs`](../../../Content.Goobstation.Shared/Enchanting/Components/BonusDamageEnchantComponent.cs) |
| [`Weapons`](../../../Content.Goobstation.Shared/Weapons) | 30 | [`CounterattackWeaponComponent.cs`](../../../Content.Goobstation.Shared/Weapons/CounterattackWeapon/CounterattackWeaponComponent.cs) |
| [`Blob`](../../../Content.Goobstation.Shared/Blob) | 29 | [`BlobCarrierSystem.cs`](../../../Content.Goobstation.Shared/Blob/BlobCarrierSystem.cs) |
| [`Changeling`](../../../Content.Goobstation.Shared/Changeling) | 25 | [`Changeling.Actions.cs`](../../../Content.Goobstation.Shared/Changeling/Actions/Changeling.Actions.cs) |
| [`Disease`](../../../Content.Goobstation.Shared/Disease) | 25 | [`ImmunityModifierMetabolismComponent.cs`](../../../Content.Goobstation.Shared/Disease/Chemistry/ImmunityModifierMetabolismComponent.cs) |
| [`Xenobiology`](../../../Content.Goobstation.Shared/Xenobiology) | 24 | [`BreedPrototype.cs`](../../../Content.Goobstation.Shared/Xenobiology/BreedPrototype.cs) |
| [`Clothing`](../../../Content.Goobstation.Shared/Clothing) | 22 | [`AutoInjectEvents.cs`](../../../Content.Goobstation.Shared/Clothing/AutoInjectEvents.cs) |
| [`SlaughterDemon`](../../../Content.Goobstation.Shared/SlaughterDemon) | 18 | [`BloodCrawl.Events.cs`](../../../Content.Goobstation.Shared/SlaughterDemon/BloodCrawl.Events.cs) |
| [`EntityEffects`](../../../Content.Goobstation.Shared/EntityEffects) | 17 | [`AddReagentToBlood.cs`](../../../Content.Goobstation.Shared/EntityEffects/Effects/AddReagentToBlood.cs) |
| [`Religion`](../../../Content.Goobstation.Shared/Religion) | 17 | [`AltarSourceComponent.cs`](../../../Content.Goobstation.Shared/Religion/AltarSourceComponent.cs) |
| [`StationRadio`](../../../Content.Goobstation.Shared/StationRadio) | 12 | [`ActiveVinylComponent.cs`](../../../Content.Goobstation.Shared/StationRadio/Components/ActiveVinylComponent.cs) |
| [`Devil`](../../../Content.Goobstation.Shared/Devil) | 11 | [`Devil.Actions.cs`](../../../Content.Goobstation.Shared/Devil/Actions/Devil.Actions.cs) |
