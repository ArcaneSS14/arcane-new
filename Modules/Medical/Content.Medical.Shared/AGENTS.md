# Content.Medical.Shared project guidance

This guide describes the root project `Content.Medical.Shared/Content.Medical.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Medical.Shared/Content.Medical.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Medical.Shared` (221 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Surgery`](../../../Content.Medical.Shared/Surgery) | 81 | [`OperatingTableComponent.cs`](../../../Content.Medical.Shared/Surgery/Components/OperatingTableComponent.cs) |
| [`Body`](../../../Content.Medical.Shared/Body) | 42 | [`BodyCacheComponent.cs`](../../../Content.Medical.Shared/Body/Components/BodyCacheComponent.cs) |
| [`Augments`](../../../Content.Medical.Shared/Augments) | 17 | [`AugmentActionComponent.cs`](../../../Content.Medical.Shared/Augments/Components/AugmentActionComponent.cs) |
| [`EntityEffects`](../../../Content.Medical.Shared/EntityEffects) | 17 | [`ActivateArtifact.cs`](../../../Content.Medical.Shared/EntityEffects/ActivateArtifact.cs) |
| [`Traumas`](../../../Content.Medical.Shared/Traumas) | 14 | [`AmputationTraumaComponent.cs`](../../../Content.Medical.Shared/Traumas/Components/AmputationTraumaComponent.cs) |
| [`Abductor`](../../../Content.Medical.Shared/Abductor) | 9 | [`AbductorCameraConsoleUI.cs`](../../../Content.Medical.Shared/Abductor/AbductorCameraConsoleUI.cs) |
| [`Wounds`](../../../Content.Medical.Shared/Wounds) | 7 | [`WoundComponent.cs`](../../../Content.Medical.Shared/Wounds/Components/WoundComponent.cs) |
| [`Autodoc`](../../../Content.Medical.Shared/Autodoc) | 5 | [`ActiveAutodocComponent.cs`](../../../Content.Medical.Shared/Autodoc/ActiveAutodocComponent.cs) |
| [`Restrict`](../../../Content.Medical.Shared/Restrict) | 4 | [`RestrictGunshotsByUserTag.cs`](../../../Content.Medical.Shared/Restrict/RestrictGunshotsByUserTag.cs) |
| [`Weapons`](../../../Content.Medical.Shared/Weapons) | 3 | [`CuffsOnHitComponent.cs`](../../../Content.Medical.Shared/Weapons/CuffsOnHitComponent.cs) |
| [`Abilities`](../../../Content.Medical.Shared/Abilities) | 2 | [`GoliathTentacleComponent.cs`](../../../Content.Medical.Shared/Abilities/Goliath/GoliathTentacleComponent.cs) |
| [`Cybernetics`](../../../Content.Medical.Shared/Cybernetics) | 2 | [`CyberneticsComponent.cs`](../../../Content.Medical.Shared/Cybernetics/CyberneticsComponent.cs) |
| [`DelayedDeath`](../../../Content.Medical.Shared/DelayedDeath) | 2 | [`DelayedDeathComponent.cs`](../../../Content.Medical.Shared/DelayedDeath/DelayedDeathComponent.cs) |
| [`DoAfter`](../../../Content.Medical.Shared/DoAfter) | 2 | [`DoAfterDelayMultiplierComponent.cs`](../../../Content.Medical.Shared/DoAfter/DoAfterDelayMultiplierComponent.cs) |
