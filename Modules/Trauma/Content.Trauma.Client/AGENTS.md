# Content.Trauma.Client project guidance

This guide describes the root project `Content.Trauma.Client/Content.Trauma.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Goobstation.Client`, `Content.Trauma.Shared`, `Content.Client`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Trauma.Client/Content.Trauma.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Trauma.Client` (339 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Heretic`](../../../Content.Trauma.Client/Heretic) | 61 | [`AreaMansusGraspOverlay.cs`](../../../Content.Trauma.Client/Heretic/AreaMansusGraspOverlay.cs) |
| [`Wizard`](../../../Content.Trauma.Client/Wizard) | 24 | [`WizardMirrorBoundUserInterface.cs`](../../../Content.Trauma.Client/Wizard/MagicMirror/WizardMirrorBoundUserInterface.cs) |
| [`CosmicCult`](../../../Content.Trauma.Client/CosmicCult) | 15 | [`CosmicColossusSystem.cs`](../../../Content.Trauma.Client/CosmicCult/CosmicColossusSystem.cs) |
| [`Genetics`](../../../Content.Trauma.Client/Genetics) | 13 | [`TelepathyBUI.cs`](../../../Content.Trauma.Client/Genetics/Abilities/TelepathyBUI.cs) |
| [`BloodCult`](../../../Content.Trauma.Client/BloodCult) | 10 | [`ClientBloodCultSystem.cs`](../../../Content.Trauma.Client/BloodCult/ClientBloodCultSystem.cs) |
| [`UserActions`](../../../Content.Trauma.Client/UserActions) | 10 | [`IconButton.cs`](../../../Content.Trauma.Client/UserActions/Controls/IconButton.cs) |
| [`Particles`](../../../Content.Trauma.Client/Particles) | 9 | [`ActiveEmitter.cs`](../../../Content.Trauma.Client/Particles/ActiveEmitter.cs) |
| [`CartridgeLoader`](../../../Content.Trauma.Client/CartridgeLoader) | 8 | [`LogProbeUiFragmentDeltaV.cs`](../../../Content.Trauma.Client/CartridgeLoader/Cartridges/LogProbeUiFragmentDeltaV.cs) |
| [`Nuclear`](../../../Content.Trauma.Client/Nuclear) | 8 | [`ClientNuclearMachineSystem.cs`](../../../Content.Trauma.Client/Nuclear/ClientNuclearMachineSystem.cs) |
| [`Knowledge`](../../../Content.Trauma.Client/Knowledge) | 7 | [`KnowledgeSystem.cs`](../../../Content.Trauma.Client/Knowledge/KnowledgeSystem.cs) |
| [`Spy`](../../../Content.Trauma.Client/Spy) | 7 | [`BeingScannedComponent.cs`](../../../Content.Trauma.Client/Spy/BeingScannedComponent.cs) |
| [`Viewcone`](../../../Content.Trauma.Client/Viewcone) | 7 | [`ViewconeOccludableTreeComponent.cs`](../../../Content.Trauma.Client/Viewcone/ComponentTree/ViewconeOccludableTreeComponent.cs) |
| [`Weapons`](../../../Content.Trauma.Client/Weapons) | 7 | [`MeleeBlinkSystem.cs`](../../../Content.Trauma.Client/Weapons/MeleeBlinkSystem.cs) |
| [`Stylesheets`](../../../Content.Trauma.Client/Stylesheets) | 6 | [`AlienStylesheet.Palettes.cs`](../../../Content.Trauma.Client/Stylesheets/AlienStylesheet.Palettes.cs) |

Configured output path: `../bin/Content.Client/`.
