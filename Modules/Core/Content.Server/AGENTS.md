# Content.Server project guidance

This guide describes the root project `Content.Server/Content.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Packaging`, `Content.Server.Database`, `Content.Shared.Database`, `Content.Shared`, `Content.Goobstation.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Server/Content.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Server` (2130 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`NPC`](../../../Content.Server/NPC) | 170 | [`AddNPCCommand.cs`](../../../Content.Server/NPC/Commands/AddNPCCommand.cs) |
| [`Administration`](../../../Content.Server/Administration) | 140 | [`AdminCommandAttribute.cs`](../../../Content.Server/Administration/AdminCommandAttribute.cs) |
| [`Atmos`](../../../Content.Server/Atmos) | 136 | [`AddAtmosCommand.cs`](../../../Content.Server/Atmos/Commands/AddAtmosCommand.cs) |
| [`GameTicking`](../../../Content.Server/GameTicking) | 85 | [`DelayStartCommand.cs`](../../../Content.Server/GameTicking/Commands/DelayStartCommand.cs) |
| [`Power`](../../../Content.Server/Power) | 76 | [`PowerStatCommand.cs`](../../../Content.Server/Power/Commands/PowerStatCommand.cs) |
| [`Construction`](../../../Content.Server/Construction) | 62 | [`AnchorOnlyOnStationSystem.cs`](../../../Content.Server/Construction/AnchorOnlyOnStationSystem.cs) |
| [`Objectives`](../../../Content.Server/Objectives) | 60 | [`AddObjectiveCommand.cs`](../../../Content.Server/Objectives/Commands/AddObjectiveCommand.cs) |
| [`StationEvents`](../../../Content.Server/StationEvents) | 60 | [`BasicStationEventSchedulerSystem.cs`](../../../Content.Server/StationEvents/BasicStationEventSchedulerSystem.cs) |
| [`Shuttles`](../../../Content.Server/Shuttles) | 53 | [`DelayShuttleRoundEndCommand.cs`](../../../Content.Server/Shuttles/Commands/DelayShuttleRoundEndCommand.cs) |
| [`Xenoarchaeology`](../../../Content.Server/Xenoarchaeology) | 43 | [`RandomArtifactSpriteSystem.cs`](../../../Content.Server/Xenoarchaeology/Artifact/RandomArtifactSpriteSystem.cs) |
| [`Anomaly`](../../../Content.Server/Anomaly) | 38 | [`AnomalyScannerSystem.cs`](../../../Content.Server/Anomaly/AnomalyScannerSystem.cs) |
| [`Procedural`](../../../Content.Server/Procedural) | 36 | [`DungeonAtlasTemplateComponent.cs`](../../../Content.Server/Procedural/DungeonAtlasTemplateComponent.cs) |
| [`Chemistry`](../../../Content.Server/Chemistry) | 34 | [`DumpReagentGuideText.cs`](../../../Content.Server/Chemistry/Commands/DumpReagentGuideText.cs) |
| [`Chat`](../../../Content.Server/Chat) | 31 | [`AnnounceOnSpawnComponent.cs`](../../../Content.Server/Chat/AnnounceOnSpawnComponent.cs) |

Configured output path: `../bin/Content.Server/`.
