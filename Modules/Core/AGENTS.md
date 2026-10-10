# Core content guide

This owner groups the root `Content.Common`, `Content.Shared`, `Content.Server`, and `Content.Client` projects plus databases, tests, and developer utilities. It is the shared gameplay foundation and consumes some extension modules; module-specific implementations should stay in their owner projects when an appropriate owner exists.

## Where mechanics live

- `Content.Common`: low-level contracts and reusable data such as damage types, speech, movement, inventory, and device payloads.
- `Content.Shared`: cross-side components and systems for the broad game simulation: damage, movement, weapons, chemistry, atmos, triggers/effects, shuttles, nutrition, speech, roles, and many other mechanics. It also references several module `Common` projects.
- `Content.Server`: authoritative simulation and server-only services, including NPC/HTN, round rules and events, objectives, atmos, construction, power, shuttles, anomalies, administration, and persistence integration.
- `Content.Client`: presentation and interaction for shared mechanics, including UI, administration, atmosphere displays, weapons, lobby, lighting, cargo, research, shuttles, stylesheets, and visuals.
- `Content.Shared.Database` and `Content.Server.Database`: database contracts, migrations, and persistence implementations.
- `Content.Tests` and `Content.IntegrationTests`: unit and runtime-level coverage. Tests are grouped by mechanic; integration tests also include owner-specific test directories.
- `Content.Benchmarks`, `Content.MapRenderer`, `Content.Packaging`, `Content.Replay`, `Content.Tools`, `Content.YAMLLinter`, and `Content.PatreonParser`: supporting executables, benchmark harnesses, replay/map tooling, packaging, and validation rather than gameplay ownership.

Consult each linked project guide in [`CATALOG.md`](../CATALOG.md) for observed source directories, example files, dependencies, and its direct build command. This map is based on C# source; prototypes, localization, maps, and other assets live under root `Resources`.
