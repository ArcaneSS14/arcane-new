
Determine the canonical repository, owner tag, real owner, assembly, and dependency direction before choosing a path.

## Repository ownership evidence

VERIFY:

1. repository remotes and branch when changing inherited files, ownership, project references, synchronization, or automation
2. current target branch when that branch affects the requested change
3. existing repository edit markers
4. owner module and owner underscore directories
5. nearest implementation in the narrowest relevant project or resource root
6. owning project files and resource roots; read `module.yml` only when the owner actually has one
7. caller and target assemblies
8. existing tests and extension points

Do not infer ownership from a filename or namespace alone.

## Owner-local paths

Treat these as owner-local when repository metadata confirms ownership:

- owner-specific project directories such as `Content.Arcane.*`
- verified owner-specific resource directories such as `Resources/Prototypes/_Arcane`
- projects explicitly created and maintained by the current repository

Owner-local files do not receive redundant owner edit markers.

Files inherited from an upstream repository and located outside owner-local paths receive the current repository marker around the smallest changed block, unless the change is explicitly upstream-ready.

## Dependency rules

Base content MUST NOT depend on a feature module. Module-to-module coupling MUST NOT be added for convenience. Runtime discovery is not a compile-time reference.

Moving a contract to Shared solely to bypass access is forbidden. Shared receives only state and contracts genuinely required by both sides.

## Layer ownership

| Layer | Owns | Keep out |
|---|---|---|
| `Content.Shared` | Replicated state and contracts, shared events, and deterministic behavior required by both sides or prediction | Client controls, persistence, server-only services, hidden or privileged state |
| `Content.Server` | Authoritative validation and mutation, protected outcomes, persistence coordination, and server-only simulation | Window/control code and decisions delegated to client input |
| `Content.Client` | Presentation, XAML controls, local rendering, client input, and prediction-safe feedback | Security decisions, hidden-state calculation, authoritative outcomes |

Put code in the narrowest layer that owns its dependencies. Arcane-only behavior belongs in Arcane owner projects; base projects own behavior reusable by base content. A namespace or project reference does not change these responsibilities.

For UI, keep XAML focused on layout and code-behind focused on view state and translating control events into intent. A BUI or EUI adapter owns the UI lifecycle, state binding, and message transport. Shared owns only the serializable contract needed across the boundary; it does not own the window or the authority decision.

## Access rules

A module is a separate assembly even when namespaces match.

- `partial` cannot join declarations across assemblies
- extension methods cannot read private state
- inheritance cannot bypass `private`
- `internal` requires the same assembly or explicit friendship
- runtime patching is not a normal extension point

If required behavior depends on inaccessible state, STOP and report the exact declaration, modifier, involved assemblies, and smallest reusable hook.

## Infrastructure ownership

Before creating a project, manager, fixture, loader, CI step, event bus, or MSBuild target, search for the existing owner.

A feature test belongs in the existing owner test project. Integration-test projects are test infrastructure, not runtime projects; do not add them to a runtime manifest when a repository uses one.
