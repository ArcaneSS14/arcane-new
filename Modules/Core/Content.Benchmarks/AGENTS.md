# Content.Benchmarks project guidance

This guide describes the root project `Content.Benchmarks/Content.Benchmarks.csproj`; the project remains at the repository root.

## Ownership and contents

Performance benchmark harnesses and benchmark cases; avoid production behavior ownership here.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Server`, `Content.Shared`, `Content.Tests`, `Content.IntegrationTests`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Benchmarks/Content.Benchmarks.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Benchmarks` (23 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Benchmarks) | 23 | [`ColorInterpolateBenchmark.cs`](../../../Content.Benchmarks/ColorInterpolateBenchmark.cs) |

Configured output path: `../bin/Content.Benchmarks/`.
