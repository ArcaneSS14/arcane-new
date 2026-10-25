
Select verification from the files and behavior changed.

Repository baseline:

- SDK comes from `global.json`; the current content props set `LangVersion` 14 when unset, while `RobustToolbox/MSBuild/Robust.Engine.props` sets the engine target framework and language version. Verify both declarations before changing a version-bound build claim.
- Configurations are `Debug`, `DebugOpt`, `Tools`, and `Release`, defined in the engine's `Robust.Configurations.props`. `DebugOpt` keeps `DEBUG` and tools while enabling optimizations; `Release` drops asserts and tools.
- Restore with `dotnet restore` only when dependencies changed, required assets are missing, or a requested workflow requires restore; do not restore by default.
- The build-and-test workflow builds `DebugOpt`, tests the produced `Content.Tests.dll` and `Content.IntegrationTests.dll` binaries, and separately builds `Release`. The HeisenDetector workflow instead runs `dotnet test --no-build --configuration DebugOpt` against `Content.IntegrationTests.csproj`; the YAML-linter workflow builds `Release` and runs `Content.YAMLLinter.csproj` with `--no-build`. Reproduce a named workflow's own entry point only for a requested CI-equivalent or release gate; routine changes use the affected owner and failure mode.
- `MSBuild/Content.props` sets `TreatWarningsAsErrors` for `Release`. The YAML-linter workflow passes `/p:WarningsAsErrors=` to its own Release build; treat that as a workflow-specific override, not a general claim about every MSBuild property interaction. Never suppress a warning gate merely to hide an unexplained warning.
- `Content.Tests` covers focused content tests.
- `Content.IntegrationTests` covers integrated client/server behavior. Run it with `DOTNET_gcServer=1`.
- YAML linting builds in `Release`, then runs the linter with `--no-build`.
- RSI changes require the RobustToolbox RSI validator.
- Packaging changes require `Content.Packaging`.

The pinned engine is the `RobustToolbox` submodule. Read the pin with `git submodule status` from the superproject and treat that commit, not your memory of an earlier engine, as the API source of truth. Touching the engine beyond that read, including running commands inside the submodule, requires an explicit user request as defined in `.agents/rules/engine-boundaries.md`.

Use the smallest meaningful check first, then broaden only for a confirmed uncovered risk and within the user's requested scope. For documentation or agent-instruction changes, run `git diff --check`, `git diff --stat`, and inspect final diff; validate only the affected links and paths when routing, links, or paths changed. A documentation-only change does not require claiming a full game build. A networking change should not be considered verified by YAML parsing alone.

Report exact commands, their results, and checks that could not be run. Never convert an unavailable tool into an implied success.
