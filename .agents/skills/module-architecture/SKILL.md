---
name: module-architecture
description: Prove repository ownership, module ownership, assembly boundaries, edit-marker rules, extension points, and test placement.
---

## Mandatory workflow

1. verify remotes, branch, and repository owner only when ownership, inherited files, synchronization, or repository automation is in scope
2. identify the owner from the target project or resource path and its metadata
3. classify inherited paths per `.agents/rules/fork-trajectory-priority.md`
4. search the narrowest relevant project or resource root for existing behavior
5. read owning project files, applicable scoped guidance, and solution entries; inspect `module.yml` only when that owner uses one
6. identify target and caller assemblies
7. locate every required declaration and access modifier
8. verify project-reference direction
9. find existing tests, fixtures, CI steps, and resource roots
10. choose Common, Shared, Server, Client, or Resources from actual dependencies

## Trajectory

TraumaStation is the sync trajectory for inherited code in this repository. Read this repository's `CONTRIBUTING.md` for Arcane ownership and contributor conventions; consult the focused rules for inherited-file placement and markers.

Arcane runtime code lives in the root-level `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client` projects. Arcane resources live in existing `_Arcane` owner directories under `Resources`. Keep Arcane-only behavior there. Trauma-owned code and vanilla root paths are sync-sensitive; keep any required edits minimal and marked. Foreign fork paths are not Arcane-owned just because they are present in this repository.

Classification details: `.agents/rules/fork-trajectory-priority.md`.

## Edit-marker boundary

Owner-local module and underscore paths do not receive redundant owner edit markers.

Inherited files outside those paths receive the current repository marker around the smallest changed block, unless the change is explicitly upstream-ready.

Do not use a foreign marker or add comments to invalid formats.

## Assembly decisions

Use Common for contracts required below gameplay Shared. Use Shared only for replicated contracts and prediction-safe state. Use Server for authority and hidden state. Use Client for presentation and UI.

Do not move code to Shared to bypass access. Do not introduce base-to-module references. Do not add module-to-module references merely to compile.

When a required member is inaccessible, STOP and identify the smallest public extension point. Do not copy private implementation or use reflection as an unrequested workaround.

## Existing infrastructure

Before creating anything, search for an existing project, integration-test project, fixture, manifest entry when applicable, CI step, MSBuild target, manager, system, event, or service.

Build the smallest affected project and use the existing `Content.Tests` or `Content.IntegrationTests` project when it owns coverage for the changed behavior. Do not assume a separate Arcane integration-test project exists.
