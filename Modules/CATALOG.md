# Content project catalog

This catalog covers every root-level `Content.*` project file in the repository, including runtime layers, tests, and developer utilities. Guidance files live under `Modules/`; runtime source and project files remain in their existing root directories. Each row links to the project-specific guide.

## How project builds compose

A direct `dotnet build <project>.csproj --no-restore` builds that project and its declared project references, not reverse dependents. Build affected consumers too when changing a shared API. `SpaceStation14.slnx` includes the runtime, test, tooling, and utility projects shown here; solution membership is separate from whether a direct project build compiles it.

## Projects

| Root project | Owner / purpose | Direct project references | Project guide |
| --- | --- | --- | --- |
| `Content.Arcane.Client` | Arcane: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Client`, `Content.Arcane.Shared` | [Guide](Arcane/Content.Arcane.Client/AGENTS.md) |
| `Content.Arcane.Common` | Arcane: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](Arcane/Content.Arcane.Common/AGENTS.md) |
| `Content.Arcane.Server` | Arcane: Server-side authoritative behavior for this owner | `Content.Arcane.Shared`, `Content.Server` | [Guide](Arcane/Content.Arcane.Server/AGENTS.md) |
| `Content.Arcane.Shared` | Arcane: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Shared`, `Content.Arcane.Common` | [Guide](Arcane/Content.Arcane.Shared/AGENTS.md) |
| `Content.Benchmarks` | Core: Performance benchmark harnesses and benchmark cases | `Content.Client`, `Content.Server`, `Content.Shared`, `Content.Tests`, `Content.IntegrationTests` | [Guide](Core/Content.Benchmarks/AGENTS.md) |
| `Content.Client` | Core: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Goobstation.Shared`, `Content.Goobstation.UIKit`, `Content.Shared` | [Guide](Core/Content.Client/AGENTS.md) |
| `Content.Common` | Core: Low-level shared types and contracts for this owner | — | [Guide](Core/Content.Common/AGENTS.md) |
| `Content.Factory.Client` | Factory: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Client`, `Content.Factory.Shared` | [Guide](Factory/Content.Factory.Client/AGENTS.md) |
| `Content.Factory.Common` | Factory: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](Factory/Content.Factory.Common/AGENTS.md) |
| `Content.Factory.Server` | Factory: Server-side authoritative behavior for this owner | `Content.Factory.Shared`, `Content.Server` | [Guide](Factory/Content.Factory.Server/AGENTS.md) |
| `Content.Factory.Shared` | Factory: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Shared` | [Guide](Factory/Content.Factory.Shared/AGENTS.md) |
| `Content.Goobstation.Client` | Goobstation: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Client`, `Content.Goobstation.Shared` | [Guide](GoobStation/Content.Goobstation.Client/AGENTS.md) |
| `Content.Goobstation.Common` | Goobstation: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](GoobStation/Content.Goobstation.Common/AGENTS.md) |
| `Content.Goobstation.Server` | Goobstation: Server-side authoritative behavior for this owner | `Content.Common`, `Content.Goobstation.Shared`, `Content.Lavaland.Shared`, `Content.Server` | [Guide](GoobStation/Content.Goobstation.Server/AGENTS.md) |
| `Content.Goobstation.Shared` | Goobstation: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Shared`, `Content.Medical.Shared` | [Guide](GoobStation/Content.Goobstation.Shared/AGENTS.md) |
| `Content.Goobstation.UIKit` | Goobstation: Reusable UI toolkit components and shared UI-facing contracts owned by this project. | `Content.Shared` | [Guide](GoobStation/Content.Goobstation.UIKit/AGENTS.md) |
| `Content.IntegrationTests` | Core: Integration tests spanning content assemblies and runtime behavior. | `Content.Common`, `Content.Goobstation.Client`, `Content.Goobstation.Server`, `Content.Goobstation.Shared`, `Content.Trauma.Client`, `Content.Trauma.Server`, `Content.Trauma.Shared`, `Content.Factory.Client`, `Content.Factory.Server`, `Content.Lavaland.Client`, `Content.Lavaland.Server`, `Content.Medical.Client`, `Content.Medical.Server`, `Content.Client`, `Content.Server`, `Content.Shared`, `Content.Tests` | [Guide](Core/Content.IntegrationTests/AGENTS.md) |
| `Content.Lavaland.Client` | Lavaland: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Client`, `Content.Lavaland.Shared` | [Guide](Lavaland/Content.Lavaland.Client/AGENTS.md) |
| `Content.Lavaland.Common` | Lavaland: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](Lavaland/Content.Lavaland.Common/AGENTS.md) |
| `Content.Lavaland.Server` | Lavaland: Server-side authoritative behavior for this owner | `Content.Lavaland.Shared`, `Content.Server` | [Guide](Lavaland/Content.Lavaland.Server/AGENTS.md) |
| `Content.Lavaland.Shared` | Lavaland: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Shared` | [Guide](Lavaland/Content.Lavaland.Shared/AGENTS.md) |
| `Content.MapRenderer` | Core: Map rendering utility that consumes integration-test/content infrastructure | `Content.IntegrationTests` | [Guide](Core/Content.MapRenderer/AGENTS.md) |
| `Content.Medical.Client` | Medical: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Client`, `Content.Medical.Shared` | [Guide](Medical/Content.Medical.Client/AGENTS.md) |
| `Content.Medical.Common` | Medical: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](Medical/Content.Medical.Common/AGENTS.md) |
| `Content.Medical.Server` | Medical: Server-side authoritative behavior for this owner | `Content.Medical.Shared`, `Content.Server` | [Guide](Medical/Content.Medical.Server/AGENTS.md) |
| `Content.Medical.Shared` | Medical: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Shared` | [Guide](Medical/Content.Medical.Shared/AGENTS.md) |
| `Content.Packaging` | Core: Packaging executable and packaging-related build behavior. | — | [Guide](Core/Content.Packaging/AGENTS.md) |
| `Content.PatreonParser` | Core: Standalone parser utility for Patreon data. | — | [Guide](Core/Content.PatreonParser/AGENTS.md) |
| `Content.Replay` | Core: Replay executable and replay-related content integration. | `Content.Shared`, `Content.Client` | [Guide](Core/Content.Replay/AGENTS.md) |
| `Content.Server` | Core: Server-side authoritative behavior for this owner | `Content.Packaging`, `Content.Server.Database`, `Content.Shared.Database`, `Content.Shared`, `Content.Goobstation.Shared` | [Guide](Core/Content.Server/AGENTS.md) |
| `Content.Server.Database` | Core: Server persistence models and database integration. | `Content.Shared.Database` | [Guide](Core/Content.Server.Database/AGENTS.md) |
| `Content.Shared` | Core: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Trauma.Common`, `Content.Goobstation.Common`, `Content.Lavaland.Common`, `Content.Medical.Common`, `Content.Factory.Common`, `Content.Common`, `Content.Shared.Database` | [Guide](Core/Content.Shared/AGENTS.md) |
| `Content.Shared.Database` | Core: Database contracts shared between server-side persistence consumers. | — | [Guide](Core/Content.Shared.Database/AGENTS.md) |
| `Content.Tests` | Core: Unit tests for content assemblies | `Content.Common`, `Content.Trauma.Client`, `Content.Trauma.Server`, `Content.Client`, `Content.Server`, `Content.Shared` | [Guide](Core/Content.Tests/AGENTS.md) |
| `Content.Tools` | Core: Repository developer tooling | — | [Guide](Core/Content.Tools/AGENTS.md) |
| `Content.Trauma.Client` | Trauma: Client-side presentation, visuals, UI, XAML, input, and feedback for this owner | `Content.Goobstation.Client`, `Content.Trauma.Shared`, `Content.Client` | [Guide](Trauma/Content.Trauma.Client/AGENTS.md) |
| `Content.Trauma.Common` | Trauma: Low-level shared types and contracts for this owner | `Content.Common` | [Guide](Trauma/Content.Trauma.Common/AGENTS.md) |
| `Content.Trauma.Server` | Trauma: Server-side authoritative behavior for this owner | `Content.Trauma.Shared`, `Content.Goobstation.Server`, `Content.Server` | [Guide](Trauma/Content.Trauma.Server/AGENTS.md) |
| `Content.Trauma.Shared` | Trauma: Shared gameplay contracts, components, systems, and behavior for this owner | `Content.Goobstation.Shared`, `Content.Shared` | [Guide](Trauma/Content.Trauma.Shared/AGENTS.md) |
| `Content.YAMLLinter` | Core: YAML validation executable and its content-aware linting rules. | `Content.Client`, `Content.Server`, `Content.Shared`, `Content.IntegrationTests` | [Guide](Core/Content.YAMLLinter/AGENTS.md) |

## Module and resource guidance

Use the owner guides for a quick subsystem overview, then the linked project guides for source-directory maps and representative files:

- [Core](Core/AGENTS.md): shared foundation, server/client runtime, persistence, tests, and developer utilities.
- [Arcane](Arcane/AGENTS.md): Arcane-only projects and resources.
- [Trauma](Trauma/AGENTS.md): inherited TraumaStation mechanics and sync ownership.
- [GoobStation](GoobStation/AGENTS.md): inherited GoobStation mechanics and UI toolkit.
- [Lavaland](Lavaland/AGENTS.md): megafauna, procedural maps, tendrils, and related mechanics.
- [Medical](Medical/AGENTS.md): body, wounds, surgery, augments, and abductor systems.
- [Factory](Factory/AGENTS.md): circuits, automation, filters, plumbing, and machinery.

The project guides describe C# source only; prototypes, localization, maps, and assets live under root `Resources`. `Modules/Arcane/Resources/` is guidance-only; Arcane runtime assets remain in root `Resources/**/_Arcane/**`. All owner directories under `Modules/` are documentation locations; runtime projects remain at the repository root.
