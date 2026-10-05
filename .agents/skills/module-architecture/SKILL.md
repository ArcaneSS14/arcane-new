---
name: module-architecture
description: Prove repository ownership, module ownership, assembly boundaries, edit-marker rules, extension points, and test placement.
---

## Mandatory workflow

1. verify origin, upstream, branch, and repository owner tag
2. identify owner module and verified underscore owner paths
3. classify the target path as Arcane owner-local, vanilla unmarked, Trauma owner-local, or foreign fork, per `.agents/rules/fork-trajectory-priority.md`
4. search root content and every module for the existing behavior
5. read owner manifests, project files, scoped guidance, and solution entries
6. identify target and caller assemblies
7. locate every required declaration and access modifier
8. verify project-reference direction
9. find existing tests, fixtures, CI steps, and resource roots
10. choose Common, Shared, Server, Client, or Resources from actual dependencies

## Trajectory

`arcane-new` is a fork of TraumaStation, and TraumaStation is the sync source. `CONTRIBUTING.md` is the inherited TraumaStation guide and is authoritative for how Trauma-owned code is written: new C# in `Content.Trauma.*`, `.Trauma.cs` partials for additions to base files, no new handlers on upstream systems, resources under `_Trauma`, and partial prototypes in `Resources/Prototypes/_Trauma/Partials`.

Trauma-owned code and vanilla space-station-14 root paths are the conflict surface: keep edits there minimal. Arcane-only behavior goes in `Modules/Arcane`. Foreign fork paths such as `Modules/GoobStation/**`, `Resources/_Goobstation/**`, and `Resources/_EinsteinEngines/**` are not part of this trajectory, so the minimization requirement is weaker there.

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

Before creating anything, search for an existing module project, integration-test project, fixture, manifest entry, CI step, MSBuild target, manager, system, event, or service.

Build the affected project graph and run the existing owner test project.
