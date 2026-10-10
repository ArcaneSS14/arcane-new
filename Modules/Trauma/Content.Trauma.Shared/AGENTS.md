# Content.Trauma.Shared project guidance

This guide describes the root project `Content.Trauma.Shared/Content.Trauma.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Goobstation.Shared`, `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Trauma.Shared/Content.Trauma.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Trauma.Shared` (1727 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Heretic`](../../../Content.Trauma.Shared/Heretic) | 264 | [`BaseSpriteOverlayComponent.cs`](../../../Content.Trauma.Shared/Heretic/Components/BaseSpriteOverlayComponent.cs) |
| [`EntityEffects`](../../../Content.Trauma.Shared/EntityEffects) | 116 | [`AccumulateCounterStatusEffect.cs`](../../../Content.Trauma.Shared/EntityEffects/AccumulateCounterStatusEffect.cs) |
| [`Wizard`](../../../Content.Trauma.Shared/Wizard) | 87 | [`ApprenticeComponent.cs`](../../../Content.Trauma.Shared/Wizard/ApprenticeComponent.cs) |
| [`BloodCult`](../../../Content.Trauma.Shared/BloodCult) | 73 | [`BloodBoilProjectileComponent.cs`](../../../Content.Trauma.Shared/BloodCult/BloodBoilProjectile/BloodBoilProjectileComponent.cs) |
| [`Genetics`](../../../Content.Trauma.Shared/Genetics) | 71 | [`ArmorMutationComponent.cs`](../../../Content.Trauma.Shared/Genetics/Abilities/ArmorMutationComponent.cs) |
| [`CosmicCult`](../../../Content.Trauma.Shared/CosmicCult) | 52 | [`CosmicDamageTransferSystem.cs`](../../../Content.Trauma.Shared/CosmicCult/Abilities/CosmicDamageTransferSystem.cs) |
| [`Vampires`](../../../Content.Trauma.Shared/Vampires) | 43 | [`ActionLairComponent.cs`](../../../Content.Trauma.Shared/Vampires/ActionLairComponent.cs) |
| [`Knowledge`](../../../Content.Trauma.Shared/Knowledge) | 39 | [`AimSpeedKnowledgeComponent.cs`](../../../Content.Trauma.Shared/Knowledge/Components/AimSpeedKnowledgeComponent.cs) |
| [`Weapons`](../../../Content.Trauma.Shared/Weapons) | 38 | [`SelectableAmmoSystem.cs`](../../../Content.Trauma.Shared/Weapons/AmmoSelector/SelectableAmmoSystem.cs) |
| [`Ranching`](../../../Content.Trauma.Shared/Ranching) | 36 | [`AddComponentOnHappyComponent.cs`](../../../Content.Trauma.Shared/Ranching/Components/AddComponentOnHappyComponent.cs) |
| [`EntityConditions`](../../../Content.Trauma.Shared/EntityConditions) | 32 | [`AllConditions.cs`](../../../Content.Trauma.Shared/EntityConditions/AllConditions.cs) |
| [`Body`](../../../Content.Trauma.Shared/Body) | 29 | [`OrganChipComponent.cs`](../../../Content.Trauma.Shared/Body/Chips/OrganChipComponent.cs) |
| [`Xenomorphs`](../../../Content.Trauma.Shared/Xenomorphs) | 26 | [`AcidCorrodingComponent.cs`](../../../Content.Trauma.Shared/Xenomorphs/Acid/Components/AcidCorrodingComponent.cs) |
| [`Nuclear`](../../../Content.Trauma.Shared/Nuclear) | 25 | [`ActiveNuclearCentrifugeComponent.cs`](../../../Content.Trauma.Shared/Nuclear/Centrifuge/ActiveNuclearCentrifugeComponent.cs) |
