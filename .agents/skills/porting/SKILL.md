---
name: porting
description: Port one complete feature family with provenance, current-API adaptation, repository ownership, localization, resources, and verification.
---

Use one destination change per feature family. Port final intended behavior, including later fixes, rather than historical broken states.

## Destination

A port lands in Arcane-owned paths: `Modules/Arcane/**`, `Content.Arcane.*`, and `Modules/Lavaland/**` where the module's scope fits. A port pasted into a vanilla root path or a Trauma-owned path becomes a recurring reconciliation cost, and a port is large enough for that cost to be real.

Read `Modules/Arcane/AGENTS.md` before writing there. Respect its dependency direction: Common may not depend on Arcane Shared, Server, or Client. Do not add a project reference to make a port compile; move the code to the correct layer instead.

The one exception is a change to an existing base type that the port cannot avoid. Then the diff is the smallest that works, wrapped in Arcane markers, and named in the delivery note. Full routing table: `.agents/rules/port-destination.md`.

## Provenance

Record the source repository, source commit, root PR, follow-ups, exclusions, dependencies, and licensing evidence. Inventory code layers, prototypes, English, Russian, UI, maps, sprites, audio, tests, database changes, CVars, and every inherited-file modification.

Verify the destination repository identity, owner tag, owner module, owner underscore paths, current APIs, project references, resources, and test infrastructure.

Do not assume a source API, path, field, prototype parent, test project, marker, or locale layout exists in the destination.

A source fork's marker does not travel. A port gets Arcane markers, never `Trauma - `, `<Trauma>`, or `Goobstation-`.

## Ownership

Repository-owned behavior belongs in owner-local paths when possible. Inherited edits require the destination repository marker around the smallest delta. Owner-local module and underscore paths do not receive redundant markers.

Do not bundle independent systems or create duplicate build and test infrastructure.

## Localization

English localization is structurally canonical. Add natural Russian localization and mirror English keys, attributes, variables, selectors, file paths, and order. An English-only key is not a follow-up item.

## Verification

Run the DebugOpt build of the smallest affected Arcane project, Release resource validation, and the existing owner test project. Build a base project too when the port edited a base file. Do not create new test infrastructure for a port.

Report omitted source behavior, unavailable checks, and the count of base-path files edited.
