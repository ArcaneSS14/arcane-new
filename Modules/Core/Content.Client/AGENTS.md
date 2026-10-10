# Content.Client project guidance

This guide describes the root project `Content.Client/Content.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Goobstation.Shared`, `Content.Goobstation.UIKit`, `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Client/Content.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Client` (1560 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`UserInterface`](../../../Content.Client/UserInterface) | 134 | [`BoundKeyHelpers.cs`](../../../Content.Client/UserInterface/BoundKeyHelpers.cs) |
| [`Administration`](../../../Content.Client/Administration) | 93 | [`AdminNameOverlay.cs`](../../../Content.Client/Administration/AdminNameOverlay.cs) |
| [`Stylesheets`](../../../Content.Client/Stylesheets) | 77 | [`BaseStylesheet.Fonts.cs`](../../../Content.Client/Stylesheets/BaseStylesheet.Fonts.cs) |
| [`Atmos`](../../../Content.Client/Atmos) | 74 | [`AlignAtmosPipeLayers.cs`](../../../Content.Client/Atmos/AlignAtmosPipeLayers.cs) |
| [`Shuttles`](../../../Content.Client/Shuttles) | 34 | [`EmergencyConsoleBoundUserInterface.cs`](../../../Content.Client/Shuttles/BUI/EmergencyConsoleBoundUserInterface.cs) |
| [`Lobby`](../../../Content.Client/Lobby) | 33 | [`ClientPreferencesManager.cs`](../../../Content.Client/Lobby/ClientPreferencesManager.cs) |
| [`Light`](../../../Content.Client/Light) | 31 | [`AfterLightTargetOverlay.cs`](../../../Content.Client/Light/AfterLightTargetOverlay.cs) |
| [`Power`](../../../Content.Client/Power) | 31 | [`ApcBoundUserInterface.cs`](../../../Content.Client/Power/APC/ApcBoundUserInterface.cs) |
| [`Weapons`](../../../Content.Client/Weapons) | 31 | [`DamageMarkerSystem.cs`](../../../Content.Client/Weapons/Marker/DamageMarkerSystem.cs) |
| [`Guidebook`](../../../Content.Client/Guidebook) | 28 | [`GuideHelpComponent.cs`](../../../Content.Client/Guidebook/Components/GuideHelpComponent.cs) |
| [`Silicons`](../../../Content.Client/Silicons) | 28 | [`BorgBoundUserInterface.cs`](../../../Content.Client/Silicons/Borgs/BorgBoundUserInterface.cs) |
| [`Chemistry`](../../../Content.Client/Chemistry) | 24 | [`SolutionItemStatusComponent.cs`](../../../Content.Client/Chemistry/Components/SolutionItemStatusComponent.cs) |
| [`Overlays`](../../../Content.Client/Overlays) | 22 | [`BlackAndWhiteOverlay.cs`](../../../Content.Client/Overlays/BlackAndWhiteOverlay.cs) |
| [`CartridgeLoader`](../../../Content.Client/CartridgeLoader) | 18 | [`CartridgeLoaderBoundUserInterface.cs`](../../../Content.Client/CartridgeLoader/CartridgeLoaderBoundUserInterface.cs) |

Configured output path: `../bin/Content.Client/`.
