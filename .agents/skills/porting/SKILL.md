---
name: porting
description: Port one complete feature family with provenance, current-API adaptation, repository ownership, localization, resources, and verification.
---

Use one destination change per feature family. Port final intended behavior, including later fixes, rather than historical broken states.

## Destination

A port or cherry-pick lands in the corresponding Arcane-owned project or resource path by default: the root-level `Content.Arcane.*` projects and existing Arcane-owned resource directories such as `Resources/Prototypes/_Arcane`, `Resources/Textures/_Arcane`, and `Resources/Locale/{culture}/_Arcane`. This exclusive ownership reduces conflicts with other forks and upstream changes. Here, “Arcane module” means those projects and resource paths, not `Modules/Arcane`, which contains guidance. Exception: when source history confirms the change was added by TraumaStation, retain it on its corresponding Trauma-owned or vanilla path so future syncs can carry it forward. Keep Arcane-specific adaptations in Arcane-owned paths. Follow `.agents/rules/port-destination.md` for provenance and destination decisions.

If a port restores a file or behavior removed by a TraumaStation revert or other Trauma-side deletion, place the restored implementation in Arcane-owned paths, including when it is a newly added file. This restoration rule takes precedence over the ordinary TraumaStation-addition exception: do not put the removed content back into a Trauma-owned file and create a repeat sync conflict. Trace the original addition, deletion/revert, and follow-ups; adapt the intended behavior to current APIs. Make only minimal, Arcane-marked changes to inherited files when integration cannot be done from Arcane-owned code.

Read root `AGENTS.md` and any scoped guidance that applies to the actual project or resource path. Respect the project dependency direction in `CONTRIBUTING.md`: Common may not depend on Arcane Shared, Server, or Client. Do not add a project reference to make a port compile; move the code to the correct layer instead.

If an Arcane-specific port must change an existing base type, keep the diff to the smallest working change, wrap it in Arcane markers, and name it in the delivery note. The TraumaStation provenance exception is separate: use the matching sync-trajectory path for a proven TraumaStation addition.

## Provenance

Before choosing the port's final state, follow `.agents/rules/change-history-analysis.md`. Trace the source base/head, introducing change, follow-up fixes, reverts/restorations, and current state by exact commits and affected paths; inspect actual diffs, not only titles.

Record the source repository, source commit, root PR, follow-ups, exclusions, dependencies, and licensing evidence. Inventory code layers, prototypes, English localization, UI, maps, sprites, audio, tests, database changes, CVars, additions/removals, and every inherited-file modification. Include Russian localization only when explicitly in scope.

Verify the destination repository identity, owner tag, owner module, owner underscore paths, current APIs, project references, resources, and test infrastructure.

Do not assume a source API, path, field, prototype parent, test project, marker, or locale layout exists in the destination.

A source fork's marker does not travel. A port gets Arcane markers, never `Trauma - `, `<Trauma>`, or `Goobstation-`.

## Ownership

Repository-owned behavior belongs in owner-local paths when possible. Inherited edits require the destination repository marker around the smallest delta. Owner-local module and underscore paths do not receive redundant markers.

Do not bundle independent systems or create duplicate build and test infrastructure.

## Localization

English localization is structurally canonical and required by default. Do not add or synchronize Russian localization as an automatic part of a port. If Russian is explicitly requested, preserve matching keys, attributes, variables, selectors, paths, and ordering with natural wording.

## Verification

Build the smallest affected Arcane project and run the existing owner test project when it covers the port. Run targeted resource validation for resource changes. Build a base project too when the port edited a base file. Do not create new test infrastructure for a port.

Report omitted source behavior, unavailable checks, and the count of base-path files edited.
