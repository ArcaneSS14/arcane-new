# Content.Medical.Common project guidance

This guide describes the root project `Content.Medical.Common/Content.Medical.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: `Content.Common`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Medical.Common/Content.Medical.Common.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Medical.Common` (33 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Body`](../../../Content.Medical.Common/Body) | 15 | [`BloodstreamUpdateEvent.cs`](../../../Content.Medical.Common/Body/BloodstreamUpdateEvent.cs) |
| [`Targeting`](../../../Content.Medical.Common/Targeting) | 3 | [`Events.cs`](../../../Content.Medical.Common/Targeting/Events.cs) |
| [`Damage`](../../../Content.Medical.Common/Damage) | 2 | [`DamageBehaviors.cs`](../../../Content.Medical.Common/Damage/DamageBehaviors.cs) |
| [`Surgery`](../../../Content.Medical.Common/Surgery) | 2 | [`Events.cs`](../../../Content.Medical.Common/Surgery/Events.cs) |
| [`(root)`](../../../Content.Medical.Common) | 1 | [`GlobalUsings.cs`](../../../Content.Medical.Common/GlobalUsings.cs) |
| [`CCVar`](../../../Content.Medical.Common/CCVar) | 1 | [`SurgeryCVars.cs`](../../../Content.Medical.Common/CCVar/SurgeryCVars.cs) |
| [`Clothing`](../../../Content.Medical.Common/Clothing) | 1 | [`CheckEquipmentPartEvent.cs`](../../../Content.Medical.Common/Clothing/CheckEquipmentPartEvent.cs) |
| [`DoAfter`](../../../Content.Medical.Common/DoAfter) | 1 | [`ModifyDoAfterDelayEvent.cs`](../../../Content.Medical.Common/DoAfter/ModifyDoAfterDelayEvent.cs) |
| [`EntityEffects`](../../../Content.Medical.Common/EntityEffects) | 1 | [`TemperatureScaling.cs`](../../../Content.Medical.Common/EntityEffects/TemperatureScaling.cs) |
| [`Entry`](../../../Content.Medical.Common/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Medical.Common/Entry/EntryPoint.cs) |
| [`Healing`](../../../Content.Medical.Common/Healing) | 1 | [`Events.cs`](../../../Content.Medical.Common/Healing/Events.cs) |
| [`Medical`](../../../Content.Medical.Common/Medical) | 1 | [`HealthAnalyzerMessages.cs`](../../../Content.Medical.Common/Medical/HealthAnalyzer/HealthAnalyzerMessages.cs) |
| [`Traumas`](../../../Content.Medical.Common/Traumas) | 1 | [`TraumasSerializable.cs`](../../../Content.Medical.Common/Traumas/TraumasSerializable.cs) |
| [`Vomiting`](../../../Content.Medical.Common/Vomiting) | 1 | [`VomitedEvent.cs`](../../../Content.Medical.Common/Vomiting/VomitedEvent.cs) |
