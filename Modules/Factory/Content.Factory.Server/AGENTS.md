# Content.Factory.Server project guidance

This guide describes the root project `Content.Factory.Server/Content.Factory.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Factory.Shared`, `Content.Server`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Factory.Server/Content.Factory.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Factory.Server` (12 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Lathe`](../../../Content.Factory.Server/Lathe) | 2 | [`LatheAutomationComponent.cs`](../../../Content.Factory.Server/Lathe/LatheAutomationComponent.cs) |
| [`Machines`](../../../Content.Factory.Server/Machines) | 2 | [`ServerConstructorSystem.cs`](../../../Content.Factory.Server/Machines/ServerConstructorSystem.cs) |
| [`Screens`](../../../Content.Factory.Server/Screens) | 2 | [`SignalScreenComponent.cs`](../../../Content.Factory.Server/Screens/SignalScreenComponent.cs) |
| [`(root)`](../../../Content.Factory.Server) | 1 | [`GlobalUsings.cs`](../../../Content.Factory.Server/GlobalUsings.cs) |
| [`Atmos`](../../../Content.Factory.Server/Atmos) | 1 | [`GasCanisterSignalSystem.cs`](../../../Content.Factory.Server/Atmos/GasCanisterSignalSystem.cs) |
| [`Circuits`](../../../Content.Factory.Server/Circuits) | 1 | [`CircuitSystem.cs`](../../../Content.Factory.Server/Circuits/CircuitSystem.cs) |
| [`Entry`](../../../Content.Factory.Server/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Factory.Server/Entry/EntryPoint.cs) |
| [`Fax`](../../../Content.Factory.Server/Fax) | 1 | [`FaxSignalSystem.cs`](../../../Content.Factory.Server/Fax/FaxSignalSystem.cs) |
| [`Filters`](../../../Content.Factory.Server/Filters) | 1 | [`PressureFilterSystem.cs`](../../../Content.Factory.Server/Filters/PressureFilterSystem.cs) |

Configured output path: `../bin/Content.Server/`.
