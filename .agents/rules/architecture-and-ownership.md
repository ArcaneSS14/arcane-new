# Architecture and ownership

Determine the affected owner, caller/target assemblies and dependency direction. For root Content.* projects use [Modules/CATALOG.md](../../Modules/CATALOG.md) and the relevant linked guide; Modules/ is a documentation organization, not a runtime-root convention. Read a module.yml only when the actual owner uses one.

Verify remotes, target refs and provenance when inherited placement, synchronization, ownership, references or automation depends on them. A filename/namespace or remote named upstream is insufficient evidence. Routine owner-local edits do not require a complete repository identity audit.

## Placement

Use [trajectory](fork-trajectory-priority.md) for Arcane-owned, Trauma sync-sensitive, vanilla and other-fork paths and [port destination](port-destination.md) for imports/restorations. Arcane owner-local paths have no edit markers; inherited changes follow [marker syntax](arcane-edit-markers.md). There is no general unmarked upstream-ready exception.

## Layers and dependencies

| Layer | Owns | Keep out |
| --- | --- | --- |
| Common | Low-level types and contracts required below gameplay Shared, within the actual project graph | Dependencies on gameplay Shared/Server/Client; gameplay implementation that belongs in those owners |
| Shared | Common gameplay logic, contracts, replicated state and deterministic behavior required by both sides/prediction | Client controls, server-only services, persistence and hidden/privileged state |
| Server | Authoritative validation/mutation, protected outcomes, persistence and server simulation | View/control code or authority delegated to client input |
| Client | Presentation, controls, render resources, input and prediction-safe feedback | Security decisions and hidden/authoritative outcomes |

Common remains below Shared/Server/Client. Shared may use Common/base Shared, Server and Client may use Common/Shared, and Server/Client must not depend on one another. Use the narrowest owning layer and existing references. Base content must not depend on a fork feature module; do not add module-to-module coupling for convenience. Runtime discovery is not a compile-time reference. Moving a type to Shared solely to evade access is not a valid boundary decision.

XAML owns layout, code-behind view state/control intent, and a BUI/EUI adapter its entity/session lifecycle and transport. Shared serializable contracts do not own windows or server permission decisions.

## Access and infrastructure

Same namespaces do not join assemblies: partial declarations cannot span assemblies, extensions cannot read private state, inheritance cannot bypass private, and internal needs the same assembly or explicit friendship. Verify the real declaration and project reference before calling an unfamiliar API. If no supported extension exists, report the modifier, assemblies and smallest hook; [engine limits](engine-boundaries.md) still apply.

Reuse the existing system, manager, test owner, loader, CI step and MSBuild target before adding infrastructure. Tests are not runtime projects and must not be added to a runtime manifest. Detailed access/API policy: [coding-and-api-design](coding-and-api-design.md).
