<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>
SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Integration Test Guidance

This project owns integration tests and shared integration-test infrastructure for the root content and its modules. Add module-specific cases to the existing owner folder under `Tests/` (for example `_Trauma`, `_Goobstation`, or `_Lavaland`); this repository does not define separate module integration-test projects.

Use integration tests for behavior crossing server, client, networking, maps, prototypes, culture lifecycle, UI lifecycle, or multiple systems.

Control time and randomness. Assert authority first and replicated client state when relevant. Keep setup minimal and clean up entities, maps, sessions, subscriptions, configuration, and global state.

Do not duplicate PoolManager setup in module projects. Do not use runtime module loading as compile-time access. Do not add module test projects to `module.yml`.

For localization behavior, treat English as structural truth. Verify Russian parity, fallback, and culture switching only when Russian localization is explicitly in scope or the behavior under test is Russian-specific.

```powershell
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
$env:DOTNET_gcServer=1
dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed NUnit.TestOutputXml="logs" NUnit.WorkDirectory="$(pwd)/test_results"
```
