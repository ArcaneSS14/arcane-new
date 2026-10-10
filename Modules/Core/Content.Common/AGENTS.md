# Content.Common project guidance

This guide describes the root project `Content.Common/Content.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: No direct project references. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Common/Content.Common.csproj --no-restore
```

## Source area map

This map is based on the existing C# files in `Content.Common` (23 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Damage`](../../../Content.Common/Damage) | 8 | [`DamageModifierSet.Trauma.cs`](../../../Content.Common/Damage/DamageModifierSet.Trauma.cs) |
| [`Speech`](../../../Content.Common/Speech) | 3 | [`BaseAccentComponent.cs`](../../../Content.Common/Speech/Components/BaseAccentComponent.cs) |
| [`Body`](../../../Content.Common/Body) | 2 | [`OrganCategoryPrototype.Trauma.cs`](../../../Content.Common/Body/OrganCategoryPrototype.Trauma.cs) |
| [`FixedPoint`](../../../Content.Common/FixedPoint) | 2 | [`FixedPoint2.cs`](../../../Content.Common/FixedPoint/FixedPoint2.cs) |
| [`Inventory`](../../../Content.Common/Inventory) | 2 | [`InventorySystem.Relay.cs`](../../../Content.Common/Inventory/InventorySystem.Relay.cs) |
| [`(root)`](../../../Content.Common) | 1 | [`GlobalUsings.cs`](../../../Content.Common/GlobalUsings.cs) |
| [`Chat`](../../../Content.Common/Chat) | 1 | [`ChatEvents.cs`](../../../Content.Common/Chat/ChatEvents.cs) |
| [`DeviceLinking`](../../../Content.Common/DeviceLinking) | 1 | [`SignalPayload.cs`](../../../Content.Common/DeviceLinking/SignalPayload.cs) |
| [`DeviceNetwork`](../../../Content.Common/DeviceNetwork) | 1 | [`NetworkPayload.cs`](../../../Content.Common/DeviceNetwork/NetworkPayload.cs) |
| [`Entry`](../../../Content.Common/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Common/Entry/EntryPoint.cs) |
| [`Movement`](../../../Content.Common/Movement) | 1 | [`SharedMoverController.Input.cs`](../../../Content.Common/Movement/Systems/SharedMoverController.Input.cs) |
