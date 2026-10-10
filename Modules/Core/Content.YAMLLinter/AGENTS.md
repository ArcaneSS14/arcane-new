# Content.YAMLLinter project guidance

This guide describes the root project `Content.YAMLLinter/Content.YAMLLinter.csproj`; the project remains at the repository root.

## Ownership and contents

YAML validation executable and its content-aware linting rules.

## Build and dependencies

Direct project references: `Content.Client`, `Content.Server`, `Content.Shared`, `Content.IntegrationTests`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.YAMLLinter/Content.YAMLLinter.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.YAMLLinter` (1 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.YAMLLinter) | 1 | [`Program.cs`](../../../Content.YAMLLinter/Program.cs) |

Configured output path: `../bin/Content.YAMLLinter/`.
