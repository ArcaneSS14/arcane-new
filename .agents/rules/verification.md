
Select verification from the files and behavior changed.

Repository baseline:

- SDK comes from `global.json` and is .NET 10. Target framework is `net10.0` and `LangVersion` is `14`, set in `MSBuild/Content.props` and the engine's `Robust.Engine.props`. Do not target an older TFM.
- Configurations are `Debug`, `DebugOpt`, `Tools`, and `Release`, defined in the engine's `Robust.Configurations.props`. `DebugOpt` keeps `DEBUG` and tools while enabling optimizations; `Release` drops asserts and tools.
- Restore with `dotnet restore`.
- CI builds in `DebugOpt`, then tests the produced binaries, and separately builds `Release`. Reproduce the configuration the CI job uses rather than assuming `Debug`.
- `TreatWarningsAsErrors` is enabled for `Release` only. A `Release` build therefore fails on new warnings, and `/p:WarningsAsErrors=` intentionally disables that check. Never drop the flag to hide a warning you have not explained.
- `Content.Tests` covers focused content tests.
- `Content.IntegrationTests` covers integrated client/server behavior. Run it with `DOTNET_gcServer=1`.
- YAML linting builds in `Release`, then runs the linter with `--no-build`.
- RSI changes require the RobustToolbox RSI validator.
- Packaging changes require `Content.Packaging`.

The pinned engine is the `RobustToolbox` submodule. Read the pin with `git submodule status` from the superproject and treat that commit, not your memory of an earlier engine, as the API source of truth. Touching the engine beyond that read, including running commands inside the submodule, requires an explicit user request as defined in `.agents/rules/engine-boundaries.md`.

Use the smallest meaningful check first, then broaden when the change crosses assemblies or resources. A documentation-only change does not require claiming a full game build. A networking change should not be considered verified by YAML parsing alone.

Report exact commands, their results, and checks that could not be run. Never convert an unavailable tool into an implied success.
