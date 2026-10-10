<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane Shared guidance

Shared contains Arcane components, shared systems, network contracts, BUI messages and states, and prediction-compatible behavior.

Assume shared logic may run on both client and server and may run repeatedly during prediction. Keep hidden state and protected decisions on the server. Use networked fields only for information the client genuinely needs, and dirty authoritative mutations.

Do not reference Arcane Server or Client. Prefer reusable shared APIs over duplicated side-specific logic.

## Project and build impact

Runtime project: `Content.Arcane.Shared/Content.Arcane.Shared.csproj`. It references root `Content.Shared` and `Content.Arcane.Common`, and is explicitly included in `SpaceStation14.slnx`.

Build this project for Shared changes:

```sh
dotnet build Content.Arcane.Shared/Content.Arcane.Shared.csproj --no-restore
```

Arcane Server and Client both reference Shared. When a component, event, message, or other shared API changes, build the affected consumers too; a direct Shared build does not build reverse dependents.

## Source area map

This map is based on the existing C# files in `Content.Arcane.Shared` (3 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`(root)`](../../../Content.Arcane.Shared) | 1 | [`GlobalUsings.cs`](../../../Content.Arcane.Shared/GlobalUsings.cs) |
| [`CCVar`](../../../Content.Arcane.Shared/CCVar) | 1 | [`ArcaneCVars.cs`](../../../Content.Arcane.Shared/CCVar/ArcaneCVars.cs) |
| [`Entry`](../../../Content.Arcane.Shared/Entry) | 1 | [`EntryPoint.cs`](../../../Content.Arcane.Shared/Entry/EntryPoint.cs) |
