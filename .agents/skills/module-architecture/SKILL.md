---
name: module-architecture
description: Prove repository ownership, module ownership, assembly boundaries, edit-marker rules, extension points, and test placement.
---

## Когда применять

Use when a change crosses ownership, inherited paths, assemblies, project references, resource roots, modules, tests, or engine boundaries.

## До изменения

Verify repository identity/remotes/branch only for ownership, inherited-path, synchronization, automation, project-reference, or module-boundary changes. Determine the owner from the target project/resource path, `Modules/CATALOG.md`, applicable owner guide, and actual declarations. Identify caller/target assemblies, accessibility, reference direction, existing infrastructure, and applicable marker boundary.

## Правила и границы

- Arcane runtime code belongs in root `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client`; Arcane resources belong in existing `_Arcane` directories under `Resources`. `Modules/Arcane` contains guidance, not runtime projects.
- TraumaStation is the sync trajectory for inherited code. Keep Trauma-owned and vanilla root edits minimal and Arcane-marked; owner-local Arcane module/underscore paths receive no redundant marker. Never use foreign markers or an invalid comment format.
- Classify inherited paths with `.agents/rules/fork-trajectory-priority.md` and use `CONTRIBUTING.md` for Arcane ownership/contributor conventions before selecting a sync-sensitive destination.
- Choose Common for contracts below gameplay Shared, Shared for replicated/prediction-safe contracts, Server for authority/hidden state, Client for presentation/UI, and Resources for data. Do not choose a layer to bypass access.
- Verify target/caller assemblies, access modifiers, and reference direction. Never add base-to-module or Shared-to-Client/Server references, or a module-to-module reference merely to compile.
- When a member is inaccessible, find the smallest public extension point or stop with the boundary. Do not use reflection or copy private implementation.
- Reuse existing projects, owner tests, fixtures, CI steps, manifests where applicable, managers, systems, events, services, and resource roots before creating infrastructure.

## Выбор подхода

Prefer an owner-local Arcane path for Arcane-only behavior. For a sync-trajectory change, use the smallest inherited path and marker-correct block. Build/test only the existing owner project (`Content.Tests` or `Content.IntegrationTests` when it owns coverage), never assume a separate Arcane integration project exists.

## Антипаттерны

- Do not inspect repository identity for a routine owner-local edit, or create a reference solely to reach inaccessible code.
- Do not add redundant markers to owner-local paths, foreign markers to Arcane work, or broad formatting cleanup to inherited files.
- Do not create a parallel project/service/test fixture when the owner already covers it.

## Проверка

Confirm owner path, marker placement where applicable, assembly direction/access, existing test owner, and final changed paths. Use only the smallest project/build/test verification warranted by the altered surface.

## References

- [Dependency direction](references/dependency-direction.md): Read this when selecting an assembly, adding a project reference, or evaluating a cross-layer dependency.
- [Repository map](references/repository-map.md): Read this when locating an owner, runtime project, resource root, test project, or engine boundary.
