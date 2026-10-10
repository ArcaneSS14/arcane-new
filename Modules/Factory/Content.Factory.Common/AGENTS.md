# Content.Factory.Common project guidance

This guide describes the root project `Content.Factory.Common/Content.Factory.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: `Content.Common`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Factory.Common/Content.Factory.Common.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Factory.Common` (4 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Factory.Common) | 1 | [`GlobalUsings.cs`](../../../Content.Factory.Common/GlobalUsings.cs) |
| [`Construction`](../../../Content.Factory.Common/Construction) | 1 | [`ConstructedEvent.cs`](../../../Content.Factory.Common/Construction/ConstructedEvent.cs) |
| [`DeviceLinking`](../../../Content.Factory.Common/DeviceLinking) | 1 | [`LogicPayloads.cs`](../../../Content.Factory.Common/DeviceLinking/LogicPayloads.cs) |
| [`DoAfter`](../../../Content.Factory.Common/DoAfter) | 1 | [`DoAfterEndedEvent.cs`](../../../Content.Factory.Common/DoAfter/DoAfterEndedEvent.cs) |
