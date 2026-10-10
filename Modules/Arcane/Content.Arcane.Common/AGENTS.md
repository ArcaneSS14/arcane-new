<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane Common guidance

Keep this project limited to types that do not require gameplay Shared, Server, or Client assemblies.

Suitable contents include low-level identifiers, pure data shapes, and contracts required below Shared. Do not place entity systems, networked components, BUI logic, client visuals, or server authority here.

Before adding a type, confirm Arcane Shared cannot own it without creating an invalid dependency.

## Project and build impact

Runtime project: `Content.Arcane.Common/Content.Arcane.Common.csproj`. It references root `Content.Common` and is explicitly included in `SpaceStation14.slnx`.

Build this project for Common changes:

```sh
dotnet build Content.Arcane.Common/Content.Arcane.Common.csproj --no-restore
```

Arcane Shared references Common; Server and Client consume Shared. A Common API change used downstream may therefore require building those consumer projects to catch compile-time breakage. A direct Common project build does not build its reverse dependents.

## Source area map

This map is based on the existing C# files in `Content.Arcane.Common` (2 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Arcane.Common) | 1 | [`GlobalUsings.cs`](../../../Content.Arcane.Common/GlobalUsings.cs) |
| [`Entry`](../../../Content.Arcane.Common/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Arcane.Common/Entry/EntryPoint.cs) |
