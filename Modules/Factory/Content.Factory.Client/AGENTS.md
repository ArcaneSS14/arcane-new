# Content.Factory.Client project guidance

This guide describes the root project `Content.Factory.Client/Content.Factory.Client.csproj`; the project remains at the repository root.

## Ownership and contents

Client-side presentation, visuals, UI, XAML, input, and feedback for this owner; keep authoritative decisions on the server.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Factory.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Factory.Client/Content.Factory.Client.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Factory.Client` (30 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Filters`](../../../Content.Factory.Client/Filters) | 12 | [`LabelFilterBUI.cs`](../../../Content.Factory.Client/Filters/UI/LabelFilterBUI.cs) |
| [`Circuits`](../../../Content.Factory.Client/Circuits) | 9 | [`BoolPickerWindow.xaml.cs`](../../../Content.Factory.Client/Circuits/UI/BoolPickerWindow.xaml.cs) |
| [`Machines`](../../../Content.Factory.Client/Machines) | 4 | [`ClientConstructorSystem.cs`](../../../Content.Factory.Client/Machines/ClientConstructorSystem.cs) |
| [`Plumbing`](../../../Content.Factory.Client/Plumbing) | 2 | [`PlumbingFilterBUI.cs`](../../../Content.Factory.Client/Plumbing/UI/PlumbingFilterBUI.cs) |
| [`(root)`](../../../Content.Factory.Client) | 1 | [`GlobalUsings.cs`](../../../Content.Factory.Client/GlobalUsings.cs) |
| [`Entry`](../../../Content.Factory.Client/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Factory.Client/Entry/EntryPoint.cs) |
| [`Guidebook`](../../../Content.Factory.Client/Guidebook) | 1 | [`GuideAutomationSlotsEmbed.cs`](../../../Content.Factory.Client/Guidebook/Controls/GuideAutomationSlotsEmbed.cs) |

Configured output path: `../bin/Content.Client/`.
