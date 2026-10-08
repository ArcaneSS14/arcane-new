---
name: client-server-shared
description: Place contracts and behavior across authority, prediction, presentation, localization, and assembly boundaries.
---

Shared owns replicated contracts, common events, BUI contracts, and prediction-safe logic. Do not move hidden state, persistence, client controls, or server services into Shared merely for accessibility.

Server validates requests, owns protected and persistent state, selects authority-only outcomes, performs mutation, and dirties replicated state.

Client presents replicated state, renders visuals, owns controls, and provides prediction-safe feedback. Client checks are not security checks.

Player-visible feedback requires English localization and structurally ordered Russian localization. Do not network resolved strings when typed state can be localized at presentation time.

Verify repository owner, assembly boundaries, project references, and edit-marker requirements before changing inherited files.

```powershell
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
$env:DOTNET_gcServer=1
dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed NUnit.TestOutputXml="logs" NUnit.WorkDirectory="$(pwd)/test_results"
```

Run existing changed-module integration tests as applicable.
