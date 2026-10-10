# Content.Trauma.Server project guidance

This guide describes the root project `Content.Trauma.Server/Content.Trauma.Server.csproj`; the project remains at the repository root.

## Ownership and contents

Server-side authoritative behavior for this owner; validate client requests and keep client presentation out.

## Build and dependencies

Direct project references: `Content.Trauma.Shared`, `Content.Goobstation.Server`, `Content.Server`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Trauma.Server/Content.Trauma.Server.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Trauma.Server` (413 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Heretic`](../../../Content.Trauma.Server/Heretic) | 59 | [`HereticAbilitySystem.Ash.cs`](../../../Content.Trauma.Server/Heretic/Abilities/HereticAbilitySystem.Ash.cs) |
| [`Wizard`](../../../Content.Trauma.Server/Wizard) | 52 | [`AnimalAccentComponent.cs`](../../../Content.Trauma.Server/Wizard/Accents/AnimalAccentComponent.cs) |
| [`CosmicCult`](../../../Content.Trauma.Server/CosmicCult) | 32 | [`CosmicBlankSystem.cs`](../../../Content.Trauma.Server/CosmicCult/Abilities/CosmicBlankSystem.cs) |
| [`Objectives`](../../../Content.Trauma.Server/Objectives) | 19 | [`ClaimPointsConditionComponent.cs`](../../../Content.Trauma.Server/Objectives/ClaimPointsConditionComponent.cs) |
| [`Xenomorphs`](../../../Content.Trauma.Server/Xenomorphs) | 14 | [`XenomorphAcidSystem.cs`](../../../Content.Trauma.Server/Xenomorphs/Acid/XenomorphAcidSystem.cs) |
| [`JobListings`](../../../Content.Trauma.Server/JobListings) | 13 | [`BasicSideJobGeneratorComponent.cs`](../../../Content.Trauma.Server/JobListings/BasicSideJobGeneratorComponent.cs) |
| [`Language`](../../../Content.Trauma.Server/Language) | 11 | [`AdminLanguageCommand.cs`](../../../Content.Trauma.Server/Language/Commands/AdminLanguageCommand.cs) |
| [`StationEvents`](../../../Content.Trauma.Server/StationEvents) | 11 | [`CarpMigrationRuleComponent.cs`](../../../Content.Trauma.Server/StationEvents/Components/CarpMigrationRuleComponent.cs) |
| [`BloodCult`](../../../Content.Trauma.Server/BloodCult) | 10 | [`ServerSoulShardSystem.cs`](../../../Content.Trauma.Server/BloodCult/Constructs/ServerSoulShardSystem.cs) |
| [`EntityEffects`](../../../Content.Trauma.Server/EntityEffects) | 9 | [`HolyIgniteEntityEffectSystem.cs`](../../../Content.Trauma.Server/EntityEffects/HolyIgniteEntityEffectSystem.cs) |
| [`GameTicking`](../../../Content.Trauma.Server/GameTicking) | 9 | [`MapVoteOnRoundRestartSystem.cs`](../../../Content.Trauma.Server/GameTicking/MapVoteOnRoundRestartSystem.cs) |
| [`FireControl`](../../../Content.Trauma.Server/FireControl) | 7 | [`VisualizeFireDirectionsCommand.cs`](../../../Content.Trauma.Server/FireControl/Commands/VisualizeFireDirectionsCommand.cs) |
| [`Nuclear`](../../../Content.Trauma.Server/Nuclear) | 7 | [`GasTurbineMonitorSystem.cs`](../../../Content.Trauma.Server/Nuclear/Monitor/GasTurbineMonitorSystem.cs) |
| [`Spawners`](../../../Content.Trauma.Server/Spawners) | 7 | [`AreaSpawnerComponent.cs`](../../../Content.Trauma.Server/Spawners/Components/AreaSpawnerComponent.cs) |

Configured output path: `../bin/Content.Server/`.
