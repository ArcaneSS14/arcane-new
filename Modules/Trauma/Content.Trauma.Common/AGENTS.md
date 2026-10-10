# Content.Trauma.Common project guidance

This guide describes the root project `Content.Trauma.Common/Content.Trauma.Common.csproj`; the project remains at the repository root.

## Ownership and contents

Low-level shared types and contracts for this owner; keep gameplay, client presentation, and server authority out unless the existing project already owns that responsibility.

## Build and dependencies

Direct project references: `Content.Common`. A direct project build also builds its project references, but not reverse dependents. When changing an API consumed by another project, build the affected consumer project(s) as well.

Build this project with:

```sh
dotnet build Content.Trauma.Common/Content.Trauma.Common.csproj --no-restore
```


## Source area map

This map is based on the existing C# files in `Content.Trauma.Common` (224 files). It lists the most represented source directories; smaller areas and non-C# resources may not appear. Folder names identify code ownership areas, not necessarily complete feature boundaries.

| Source area | Files | Example |
| --- | ---: | --- |
| [`Wizard`](../../../Content.Trauma.Common/Wizard) | 22 | [`AfterMindSwappedEvent.cs`](../../../Content.Trauma.Common/Wizard/AfterMindSwappedEvent.cs) |
| [`LinkAccount`](../../../Content.Trauma.Common/LinkAccount) | 16 | [`ILinkAccountManager.cs`](../../../Content.Trauma.Common/LinkAccount/ILinkAccountManager.cs) |
| [`Weapons`](../../../Content.Trauma.Common/Weapons) | 10 | [`AmmoSelectorComponent.cs`](../../../Content.Trauma.Common/Weapons/AmmoSelector/AmmoSelectorComponent.cs) |
| [`CCVar`](../../../Content.Trauma.Common/CCVar) | 8 | [`TraumaCVars.BanWebhook.cs`](../../../Content.Trauma.Common/CCVar/TraumaCVars.BanWebhook.cs) |
| [`Knowledge`](../../../Content.Trauma.Common/Knowledge) | 8 | [`KnowledgeComponent.cs`](../../../Content.Trauma.Common/Knowledge/Components/KnowledgeComponent.cs) |
| [`MartialArts`](../../../Content.Trauma.Common/MartialArts) | 6 | [`BlockedBreathingComponent.cs`](../../../Content.Trauma.Common/MartialArts/BlockedBreathingComponent.cs) |
| [`Language`](../../../Content.Trauma.Common/Language) | 5 | [`LanguageSpeakerComponent.cs`](../../../Content.Trauma.Common/Language/Components/LanguageSpeakerComponent.cs) |
| [`Salvage`](../../../Content.Trauma.Common/Salvage) | 5 | [`CommonMiningPointsSystem.cs`](../../../Content.Trauma.Common/Salvage/CommonMiningPointsSystem.cs) |
| [`Body`](../../../Content.Trauma.Common/Body) | 4 | [`ModifyInhaledVolumeEvent.cs`](../../../Content.Trauma.Common/Body/ModifyInhaledVolumeEvent.cs) |
| [`Paper`](../../../Content.Trauma.Common/Paper) | 4 | [`GetSignatureEvent.cs`](../../../Content.Trauma.Common/Paper/GetSignatureEvent.cs) |
| [`Projectiles`](../../../Content.Trauma.Common/Projectiles) | 4 | [`CartridgeFiredEvent.cs`](../../../Content.Trauma.Common/Projectiles/CartridgeFiredEvent.cs) |
| [`Throwing`](../../../Content.Trauma.Common/Throwing) | 4 | [`BeforeDamageOtherOnHitEvent.cs`](../../../Content.Trauma.Common/Throwing/BeforeDamageOtherOnHitEvent.cs) |
| [`CardboardBox`](../../../Content.Trauma.Common/CardboardBox) | 3 | [`BoxAlertAttemptEvent.cs`](../../../Content.Trauma.Common/CardboardBox/BoxAlertAttemptEvent.cs) |
| [`Chat`](../../../Content.Trauma.Common/Chat) | 3 | [`ChatMessageOverrideInVoiceRangeEvent.cs`](../../../Content.Trauma.Common/Chat/ChatMessageOverrideInVoiceRangeEvent.cs) |
