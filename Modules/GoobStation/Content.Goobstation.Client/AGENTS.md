# Content.Goobstation.Client project guidance

This guide describes the root project `Content.Goobstation.Client/Content.Goobstation.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Goobstation.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Goobstation.Client/Content.Goobstation.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Goobstation.Client` (146 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Wraith`](../../../Content.Goobstation.Client/Wraith) | 11 | [`AuraSystem.cs`](../../../Content.Goobstation.Client/Wraith/Aura/AuraSystem.cs) |
| [`Blob`](../../../Content.Goobstation.Client/Blob) | 10 | [`BlobChemSwapBoundUserInterface.cs`](../../../Content.Goobstation.Client/Blob/BlobChemSwapBoundUserInterface.cs) |
| [`Xenobiology`](../../../Content.Goobstation.Client/Xenobiology) | 7 | [`MobGrowthVisualizerSystem.cs`](../../../Content.Goobstation.Client/Xenobiology/MobGrowthVisualizerSystem.cs) |
| [`Clothing`](../../../Content.Goobstation.Client/Clothing) | 6 | [`HideClothingLayerClothingComponent.cs`](../../../Content.Goobstation.Client/Clothing/Components/HideClothingLayerClothingComponent.cs) |
| [`Research`](../../../Content.Goobstation.Client/Research) | 6 | [`FancyResearchConsoleBoundUserInterface.cs`](../../../Content.Goobstation.Client/Research/UI/FancyResearchConsoleBoundUserInterface.cs) |
| [`Administration`](../../../Content.Goobstation.Client/Administration) | 5 | [`AdminInfoSystem.cs`](../../../Content.Goobstation.Client/Administration/AdminInfoSystem.cs) |
| [`Emoting`](../../../Content.Goobstation.Client/Emoting) | 5 | [`AnimatedEmotesBlacklistComponent.cs`](../../../Content.Goobstation.Client/Emoting/AnimatedEmotesBlacklistComponent.cs) |
| [`GPS`](../../../Content.Goobstation.Client/GPS) | 5 | [`CompassControl.xaml.cs`](../../../Content.Goobstation.Client/GPS/CompassControl.xaml.cs) |
| [`CustomLawboard`](../../../Content.Goobstation.Client/CustomLawboard) | 4 | [`CustomLawboardBoundInterface.cs`](../../../Content.Goobstation.Client/CustomLawboard/CustomLawboardBoundInterface.cs) |
| [`Overlays`](../../../Content.Goobstation.Client/Overlays) | 4 | [`BaseSwitchableOverlay.cs`](../../../Content.Goobstation.Client/Overlays/BaseSwitchableOverlay.cs) |
| [`Polls`](../../../Content.Goobstation.Client/Polls) | 4 | [`PollManager.cs`](../../../Content.Goobstation.Client/Polls/PollManager.cs) |
| [`Weapons`](../../../Content.Goobstation.Client/Weapons) | 4 | [`LaserPointerOverlay.cs`](../../../Content.Goobstation.Client/Weapons/LaserPointer/LaserPointerOverlay.cs) |
| [`Changeling`](../../../Content.Goobstation.Client/Changeling) | 3 | [`ChangelingBiomassSystem.cs`](../../../Content.Goobstation.Client/Changeling/ChangelingBiomassSystem.cs) |
| [`Chemistry`](../../../Content.Goobstation.Client/Chemistry) | 3 | [`EnergyReagentCardControl.xaml.cs`](../../../Content.Goobstation.Client/Chemistry/UI/EnergyReagentCardControl.xaml.cs) |

Configured output path: `../bin/Content.Client/`.
