# Content.Medical.Server project guidance

This guide describes the root project `Content.Medical.Server/Content.Medical.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Medical.Shared`, `Content.Server`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Medical.Server/Content.Medical.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Medical.Server` (14 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Abductor`](../../../Content.Medical.Server/Abductor) | 4 | [`AbductorSystem.Actions.cs`](../../../Content.Medical.Server/Abductor/AbductorSystem.Actions.cs) |
| [`Autodoc`](../../../Content.Medical.Server/Autodoc) | 2 | [`AutodocSafetyWireAction.cs`](../../../Content.Medical.Server/Autodoc/AutodocSafetyWireAction.cs) |
| [`Objectives`](../../../Content.Medical.Server/Objectives) | 2 | [`RoleplayObjectiveComponent.cs`](../../../Content.Medical.Server/Objectives/Components/RoleplayObjectiveComponent.cs) |
| [`PartStatus`](../../../Content.Medical.Server/PartStatus) | 2 | [`PartStatus.cs`](../../../Content.Medical.Server/PartStatus/PartStatus.cs) |
| [`(root)`](../../../Content.Medical.Server) | 1 | [`GlobalUsings.cs`](../../../Content.Medical.Server/GlobalUsings.cs) |
| [`Entry`](../../../Content.Medical.Server/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Medical.Server/Entry/EntryPoint.cs) |
| [`ItemSwitch`](../../../Content.Medical.Server/ItemSwitch) | 1 | [`ItemSwitchSystem.cs`](../../../Content.Medical.Server/ItemSwitch/ItemSwitchSystem.cs) |
| [`Targeting`](../../../Content.Medical.Server/Targeting) | 1 | [`TargetingSystem.cs`](../../../Content.Medical.Server/Targeting/TargetingSystem.cs) |

Configured output path: `../bin/Content.Server/`.
