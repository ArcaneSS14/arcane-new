# Content.Lavaland.Common project guidance

This guide describes the root project `Content.Lavaland.Common/Content.Lavaland.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: `Content.Common`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Lavaland.Common/Content.Lavaland.Common.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Lavaland.Common` (10 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Weapons`](../../../Content.Lavaland.Common/Weapons) | 3 | [`GetRelayMeleeWeaponEvent.cs`](../../../Content.Lavaland.Common/Weapons/GetRelayMeleeWeaponEvent.cs) |
| [`(root)`](../../../Content.Lavaland.Common) | 1 | [`GlobalUsings.cs`](../../../Content.Lavaland.Common/GlobalUsings.cs) |
| [`Chasm`](../../../Content.Lavaland.Common/Chasm) | 1 | [`BeforeChasmFallingEvent.cs`](../../../Content.Lavaland.Common/Chasm/BeforeChasmFallingEvent.cs) |
| [`Entry`](../../../Content.Lavaland.Common/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Lavaland.Common/Entry/EntryPoint.cs) |
| [`Mining`](../../../Content.Lavaland.Common/Mining) | 1 | [`UnclaimedOreComponent.cs`](../../../Content.Lavaland.Common/Mining/UnclaimedOreComponent.cs) |
| [`Mobs`](../../../Content.Lavaland.Common/Mobs) | 1 | [`FaunaComponent.cs`](../../../Content.Lavaland.Common/Mobs/FaunaComponent.cs) |
| [`Procedural`](../../../Content.Lavaland.Common/Procedural) | 1 | [`ChunkEvents.cs`](../../../Content.Lavaland.Common/Procedural/ChunkEvents.cs) |
| [`Shuttles`](../../../Content.Lavaland.Common/Shuttles) | 1 | [`RefreshShuttleConsolesEvent.cs`](../../../Content.Lavaland.Common/Shuttles/RefreshShuttleConsolesEvent.cs) |
