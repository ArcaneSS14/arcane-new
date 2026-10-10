# Content.Factory.Shared project guidance

This guide describes the root project `Content.Factory.Shared/Content.Factory.Shared.csproj`; the project remains at the repository root.

## Ownership and contents

Shared gameplay contracts, components, systems, and behavior for this owner; account for both client and server execution and prediction.

## Build and dependencies

Direct project references: `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Factory.Shared/Content.Factory.Shared.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Factory.Shared` (60 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Machines`](../../../Content.Factory.Shared/Machines) | 14 | [`AutomationSlotsComponent.cs`](../../../Content.Factory.Shared/Machines/AutomationSlotsComponent.cs) |
| [`Filters`](../../../Content.Factory.Shared/Filters) | 12 | [`AnchorFilterComponent.cs`](../../../Content.Factory.Shared/Filters/AnchorFilterComponent.cs) |
| [`Slots`](../../../Content.Factory.Shared/Slots) | 9 | [`AutomatedBeakerSlot.cs`](../../../Content.Factory.Shared/Slots/AutomatedBeakerSlot.cs) |
| [`Circuits`](../../../Content.Factory.Shared/Circuits) | 8 | [`ActiveCircuitComponent.cs`](../../../Content.Factory.Shared/Circuits/ActiveCircuitComponent.cs) |
| [`Plumbing`](../../../Content.Factory.Shared/Plumbing) | 4 | [`PlumbingFilterComponent.cs`](../../../Content.Factory.Shared/Plumbing/PlumbingFilterComponent.cs) |
| [`Access`](../../../Content.Factory.Shared/Access) | 3 | [`AccessScannerBlacklistComponent.cs`](../../../Content.Factory.Shared/Access/AccessScannerBlacklistComponent.cs) |
| [`DeviceLinking`](../../../Content.Factory.Shared/DeviceLinking) | 2 | [`SignalClockComponent.cs`](../../../Content.Factory.Shared/DeviceLinking/SignalClockComponent.cs) |
| [`Power`](../../../Content.Factory.Shared/Power) | 2 | [`SignalPowerSwitchComponent.cs`](../../../Content.Factory.Shared/Power/SignalPowerSwitchComponent.cs) |
| [`Radio`](../../../Content.Factory.Shared/Radio) | 2 | [`SignalRadioReceiverComponent.cs`](../../../Content.Factory.Shared/Radio/SignalRadioReceiverComponent.cs) |
| [`(root)`](../../../Content.Factory.Shared) | 1 | [`GlobalUsings.cs`](../../../Content.Factory.Shared/GlobalUsings.cs) |
| [`Construction`](../../../Content.Factory.Shared/Construction) | 1 | [`FlatpackSignalSystem.cs`](../../../Content.Factory.Shared/Construction/FlatpackSignalSystem.cs) |
| [`Entry`](../../../Content.Factory.Shared/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Factory.Shared/Entry/EntryPoint.cs) |
| [`Light`](../../../Content.Factory.Shared/Light) | 1 | [`LightAutomationSystem.cs`](../../../Content.Factory.Shared/Light/LightAutomationSystem.cs) |
