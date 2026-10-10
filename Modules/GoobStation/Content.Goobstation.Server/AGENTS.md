# Content.Goobstation.Server project guidance

This guide describes the root project `Content.Goobstation.Server/Content.Goobstation.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Common`, `Content.Goobstation.Shared`, `Content.Lavaland.Shared`, `Content.Server`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Goobstation.Server/Content.Goobstation.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Goobstation.Server` (277 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Devil`](../../../Content.Goobstation.Server/Devil) | 17 | [`CheatDeathSystem.cs`](../../../Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs) |
| [`Blob`](../../../Content.Goobstation.Server/Blob) | 15 | [`BlobObserverMover.cs`](../../../Content.Goobstation.Server/Blob/BlobObserverMover.cs) |
| [`Shadowling`](../../../Content.Goobstation.Server/Shadowling) | 12 | [`ShadowlingRuleSystem.cs`](../../../Content.Goobstation.Server/Shadowling/Rules/ShadowlingRuleSystem.cs) |
| [`Wraith`](../../../Content.Goobstation.Server/Wraith) | 12 | [`CurseDeathSystem.cs`](../../../Content.Goobstation.Server/Wraith/Curses/CurseDeathSystem.cs) |
| [`Changeling`](../../../Content.Goobstation.Server/Changeling) | 11 | [`ChangelingBiomassSystem.cs`](../../../Content.Goobstation.Server/Changeling/ChangelingBiomassSystem.cs) |
| [`Xenobiology`](../../../Content.Goobstation.Server/Xenobiology) | 10 | [`PickSlimeLatchTargetOperator.cs`](../../../Content.Goobstation.Server/Xenobiology/HTN/PickSlimeLatchTargetOperator.cs) |
| [`StationEvents`](../../../Content.Goobstation.Server/StationEvents) | 9 | [`JobDistributionErrorRuleComponent.cs`](../../../Content.Goobstation.Server/StationEvents/Components/JobDistributionErrorRuleComponent.cs) |
| [`Implants`](../../../Content.Goobstation.Server/Implants) | 8 | [`ComponentsImplantComponent.cs`](../../../Content.Goobstation.Server/Implants/Components/ComponentsImplantComponent.cs) |
| [`Pirates`](../../../Content.Goobstation.Server/Pirates) | 8 | [`ActivePirateRuleComponent.cs`](../../../Content.Goobstation.Server/Pirates/GameTicking/Rules/ActivePirateRuleComponent.cs) |
| [`Weapons`](../../../Content.Goobstation.Server/Weapons) | 8 | [`BatterySlotRequiresToggleComponent.cs`](../../../Content.Goobstation.Server/Weapons/BatterySlotRequiresItemToggle/BatterySlotRequiresToggleComponent.cs) |
| [`Administration`](../../../Content.Goobstation.Server/Administration) | 7 | [`AddStoreTimeCommand.cs`](../../../Content.Goobstation.Server/Administration/AddStoreTimeCommand.cs) |
| [`ChronoLegionnaire`](../../../Content.Goobstation.Server/ChronoLegionnaire) | 7 | [`StasisGunComponent.cs`](../../../Content.Goobstation.Server/ChronoLegionnaire/Components/StasisGunComponent.cs) |
| [`NTR`](../../../Content.Goobstation.Server/NTR) | 7 | [`CorporateOverrideComponent.cs`](../../../Content.Goobstation.Server/NTR/CorporateOverrideComponent.cs) |
| [`PlayerListener`](../../../Content.Goobstation.Server/PlayerListener) | 6 | [`DormNotifier.cs`](../../../Content.Goobstation.Server/PlayerListener/DormNotifier.cs) |

Configured output path: `../bin/Content.Server/`.
