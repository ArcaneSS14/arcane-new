# Content.Goobstation.UIKit project guidance

This guide describes the root project `Content.Goobstation.UIKit/Content.Goobstation.UIKit.csproj`; the project remains at the repository root.

## Ownership and contents

Reusable UI toolkit components and shared UI-facing contracts owned by this project.

## Build and dependencies

Direct project references: `Content.Shared`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Goobstation.UIKit/Content.Goobstation.UIKit.csproj --no-restore
```

## Source area map

This map is based on the existing C# files in `Content.Goobstation.UIKit` (19 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`UserInterface`](../../../Content.Goobstation.UIKit/UserInterface) | 15 | [`ShaderLabel.xaml.cs`](../../../Content.Goobstation.UIKit/UserInterface/Chat/ShaderLabel.xaml.cs) |
| [`(root)`](../../../Content.Goobstation.UIKit) | 2 | [`Assembly.cs`](../../../Content.Goobstation.UIKit/Assembly.cs) |
| [`Entry`](../../../Content.Goobstation.UIKit/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Goobstation.UIKit/Entry/EntryPoint.cs) |
| [`UserActions`](../../../Content.Goobstation.UIKit/UserActions) | 1 | [`IconButton.cs`](../../../Content.Goobstation.UIKit/UserActions/Controls/IconButton.cs) |
