# Content.IntegrationTests project guidance

This guide describes the root project `Content.IntegrationTests/Content.IntegrationTests.csproj`; the project remains at the repository root.

## Ownership and contents

Integration tests spanning content assemblies and runtime behavior.

## Build and dependencies

Direct project references: `Content.Common`, `Content.Goobstation.Client`, `Content.Goobstation.Server`, `Content.Goobstation.Shared`, `Content.Trauma.Client`, `Content.Trauma.Client`, `Content.Trauma.Server`, `Content.Trauma.Shared`, `Content.Factory.Client`, `Content.Factory.Server`, `Content.Lavaland.Client`, `Content.Lavaland.Server`, `Content.Medical.Client`, `Content.Medical.Server`, `Content.Client`, `Content.Server`, `Content.Shared`, `Content.Tests`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.IntegrationTests` (331 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Tests`](../../../Content.IntegrationTests/Tests) | 295 | [`AccessReaderTest.cs`](../../../Content.IntegrationTests/Tests/Access/AccessReaderTest.cs) |
| [`Fixtures`](../../../Content.IntegrationTests/Fixtures) | 14 | [`EnsureCVarAttribute.cs`](../../../Content.IntegrationTests/Fixtures/Attributes/EnsureCVarAttribute.cs) |
| [`(root)`](../../../Content.IntegrationTests) | 8 | [`AssemblyInfo.cs`](../../../Content.IntegrationTests/AssemblyInfo.cs) |
| [`NUnit`](../../../Content.IntegrationTests/NUnit) | 8 | [`CompConstraint.cs`](../../../Content.IntegrationTests/NUnit/Constraints/CompConstraint.cs) |
| [`Pair`](../../../Content.IntegrationTests/Pair) | 3 | [`TestPair.Helpers.cs`](../../../Content.IntegrationTests/Pair/TestPair.Helpers.cs) |
| [`Utility`](../../../Content.IntegrationTests/Utility) | 3 | [`GameDataScrounger.Files.cs`](../../../Content.IntegrationTests/Utility/GameDataScrounger.Files.cs) |

Configured output path: `../bin/Content.IntegrationTests/`.
