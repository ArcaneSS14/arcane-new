# Content.Tests project guidance

This guide describes the root project `Content.Tests/Content.Tests.csproj`; the project remains at the repository root.

## Ownership and contents

Unit tests for content assemblies; use the owner test project and existing fixtures for the behavior under test.

## Build and dependencies

Direct project references: `Content.Common`, `Content.Trauma.Client`, `Content.Trauma.Server`, `Content.Client`, `Content.Server`, `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Tests/Content.Tests.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Tests` (31 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Shared`](../../../Content.Tests/Shared) | 22 | [`AdminFlagsExtTest.cs`](../../../Content.Tests/Shared/Administration/AdminFlagsExtTest.cs) |
| [`Server`](../../../Content.Tests/Server) | 4 | [`AddMolsToMixtureTest.cs`](../../../Content.Tests/Server/Atmos/AddMolsToMixtureTest.cs) |
| [`Client`](../../../Content.Tests/Client) | 3 | [`ClickMapTest.cs`](../../../Content.Tests/Client/ClickMapTest.cs) |
| [`(root)`](../../../Content.Tests) | 2 | [`AssemblyInfo.cs`](../../../Content.Tests/AssemblyInfo.cs) |

Configured output path: `../bin/Content.Tests/`.
