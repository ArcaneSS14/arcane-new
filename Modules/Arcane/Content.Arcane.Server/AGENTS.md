<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane Server guidance

Server owns authoritative Arcane outcomes, validation, persistence coordination, hidden information, anti-abuse checks, and server-only simulation.

Validate all client messages. Re-resolve network entities and re-check state at execution time. Do not trust UI validation, displayed prices, access cached on the client, or client-selected results.

Keep presentation and XAML out of this project. Send limited BUI state or events rather than exposing complete server components.

## Project and build impact

Runtime project: `Content.Arcane.Server/Content.Arcane.Server.csproj`. It references root `Content.Server` and `Content.Arcane.Shared`, is explicitly included in `SpaceStation14.slnx`, and writes its output to `bin/Content.Server/` beside the server runtime assemblies.

Build this project for server changes:

```sh
dotnet build Content.Arcane.Server/Content.Arcane.Server.csproj --no-restore
```

This compiles the server-side dependencies but does not build Arcane Client. Changes to a shared contract used by both sides also require the affected Client build.

## Source area map

This map is based on the existing C# files in `Content.Arcane.Server` (3 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Arcane.Server) | 1 | [`GlobalUsings.cs`](../../../Content.Arcane.Server/GlobalUsings.cs) |
| [`Entry`](../../../Content.Arcane.Server/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Arcane.Server/Entry/EntryPoint.cs) |
| [`Maps`](../../../Content.Arcane.Server/Maps) | 1 | [`TileVariantNormalizationSystem.cs`](../../../Content.Arcane.Server/Maps/TileVariantNormalizationSystem.cs) |

Configured output path: `../bin/Content.Server/`.
