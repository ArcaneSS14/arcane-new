---
name: module-architecture
description: Prove repository ownership, module ownership, assembly boundaries, edit-marker rules, extension points, and test placement.
---

# Resolve an assembly or owner boundary

Locate affected project/resource ownership through [Modules/CATALOG.md](../../../Modules/CATALOG.md) and its linked guide. Inspect only project files, references, solution entries or a real module.yml needed by the decision. [Architecture](../../rules/architecture-and-ownership.md) owns layers/infrastructure; [trajectory](../../rules/fork-trajectory-priority.md) and [markers](../../rules/arcane-edit-markers.md) own inherited placement.

Identify caller and target assemblies, exact declaration/access and supported extension points. Same namespace, partial file or runtime loading does not grant cross-assembly access. Shared can own genuine common gameplay logic, not only payloads; it cannot depend on Server/Client. Do not widen references or move code to evade access.

Choose the narrowest correct Common/Shared/Server/Client/Resources owner and reuse its existing mechanism. If a required member is inaccessible, report declaration, modifier, assemblies and smallest hook rather than copying internals/reflection. [Engine limits](../../rules/engine-boundaries.md) still apply.

Select affected consumers and existing tests under [verification](../../rules/verification.md). Do not enumerate every manager, project, fixture or workflow for a local API decision.
