# Content.Medical.Client project guidance

This guide describes the root project `Content.Medical.Client/Content.Medical.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Medical.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Medical.Client/Content.Medical.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Medical.Client` (28 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Autodoc`](../../../Content.Medical.Client/Autodoc) | 6 | [`AddStepWindow.xaml.cs`](../../../Content.Medical.Client/Autodoc/AddStepWindow.xaml.cs) |
| [`Abductor`](../../../Content.Medical.Client/Abductor) | 5 | [`AbductorCameraConsoleBui.cs`](../../../Content.Medical.Client/Abductor/AbductorCameraConsoleBui.cs) |
| [`UserInterface`](../../../Content.Medical.Client/UserInterface) | 4 | [`PartStatusUIController.cs`](../../../Content.Medical.Client/UserInterface/Systems/PartStatus/PartStatusUIController.cs) |
| [`Surgery`](../../../Content.Medical.Client/Surgery) | 3 | [`SurgeryBui.cs`](../../../Content.Medical.Client/Surgery/SurgeryBui.cs) |
| [`Augments`](../../../Content.Medical.Client/Augments) | 2 | [`AugmentToolPanelMenu.xaml.cs`](../../../Content.Medical.Client/Augments/AugmentToolPanelMenu.xaml.cs) |
| [`ItemSwitch`](../../../Content.Medical.Client/ItemSwitch) | 2 | [`ItemSwitchStatusControl.cs`](../../../Content.Medical.Client/ItemSwitch/ItemSwitchStatusControl.cs) |
| [`Wounds`](../../../Content.Medical.Client/Wounds) | 2 | [`WoundableVisualsComponent.cs`](../../../Content.Medical.Client/Wounds/WoundableVisualsComponent.cs) |
| [`(root)`](../../../Content.Medical.Client) | 1 | [`GlobalUsings.cs`](../../../Content.Medical.Client/GlobalUsings.cs) |
| [`Choice`](../../../Content.Medical.Client/Choice) | 1 | [`ChoiceControl.xaml.cs`](../../../Content.Medical.Client/Choice/UI/ChoiceControl.xaml.cs) |
| [`Entry`](../../../Content.Medical.Client/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Medical.Client/Entry/EntryPoint.cs) |
| [`Targeting`](../../../Content.Medical.Client/Targeting) | 1 | [`TargetingSystem.cs`](../../../Content.Medical.Client/Targeting/TargetingSystem.cs) |

Configured output path: `../bin/Content.Client/`.
