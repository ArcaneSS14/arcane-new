<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane Client guidance

Client owns Arcane presentation, visual systems, controls, XAML, code-behind, local input presentation, and client BUI/EUI classes.

Do not make protected decisions or persist authoritative state on the client. Treat replicated data and server messages as inputs to presentation. Avoid duplicate predicted effects and clean up subscriptions, overlays, and UI lifetimes.

Keep client-only dependencies out of Shared.

## Project and build impact

Runtime project: `Content.Arcane.Client/Content.Arcane.Client.csproj`. It references root `Content.Client` and `Content.Arcane.Shared`, is explicitly included in `SpaceStation14.slnx`, imports the XamlIL build targets, and writes its output to `bin/Content.Client/` beside the client runtime assemblies.

Build this project for client or XAML changes:

```sh
dotnet build Content.Arcane.Client/Content.Arcane.Client.csproj --no-restore
```

This compiles the client-side dependencies but does not build Arcane Server. Changes to a shared contract used by both sides also require the affected Server build.

## Source area map

This map is based on the existing C# files in `Content.Arcane.Client` (2 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Arcane.Client) | 1 | [`GlobalUsings.cs`](../../../Content.Arcane.Client/GlobalUsings.cs) |
| [`Entry`](../../../Content.Arcane.Client/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Arcane.Client/Entry/EntryPoint.cs) |

Configured output path: `../bin/Content.Client/`.
