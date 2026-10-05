<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>
SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane module guidance

`Modules/Arcane` owns Arcane-specific code and resources. Root hard rules remain in force.

## Owner-local paths and edit markers

Treat `Modules/Arcane`, verified `_Arcane` paths, and Arcane-owned projects as owner-local.

Do not add `// Arcane`, `// Arcane-Edit`, `# Arcane`, `# Arcane-Edit`, or equivalent Arcane markers inside owner-local paths. These markers are only for Arcane changes to inherited files outside Arcane-owned paths.

That includes `Content.Trauma.*`, `Resources/_Trauma/**`, and `*.Trauma.cs` partials. They are owner-local for us, but they are also upstream surface for everyone syncing TraumaStation, so an Arcane change in them is marked with an Arcane marker rather than left bare. Existing practice: 11 Trauma-owned files carry `Arcane` or `Arcane-Edit` markers.

When a line we change carries an upstream marker such as `# Trauma - was 1800`, the resolved line becomes ours and carries `# Arcane-Edit: 1800 > 3000`. Leaving the upstream marker on our line would claim someone else's authorship and hide our divergence from the next sync. Upstream markers on lines we did not touch stay as they are.

When an inherited file must change, mark the smallest changed block. One added line takes a bare trailing `# Arcane` or `// Arcane`; two or more added lines take `Arcane-Start` / `Arcane-End`. One changed line takes a trailing `Arcane-Edit: <old> > <new>`; two or more changed lines take `Arcane-Edit-Start` / `Arcane-Edit-End`, and more than 5 changed lines are commented out inside that block. Never put a bare `-Start` or `-End` on a line instead of a pair.

Before editing a vanilla root path or a Trauma-owned path, look for an owner-local home instead. Syncs arrive along the Trauma trajectory, so both are the conflict surface. See `.agents/rules/fork-trajectory-priority.md` and `.agents/rules/arcane-edit-markers.md`.

## Dependency direction

- Common may depend on root `Content.Common`, but not Arcane Shared, Server, or Client.
- Shared may depend on Arcane Common and root `Content.Shared`.
- Server may depend on Arcane Common, Arcane Shared, and root `Content.Server`.
- Client may depend on Arcane Common, Arcane Shared, and root `Content.Client`.
- Arcane resources belong in `Modules/Arcane/Resources`.

Do not add root-to-Arcane dependencies or Arcane-to-foreign-module references for convenience.

## Core access

Arcane projects are separate assemblies from root Content. Verify declarations, modifiers, assemblies, and project references before using root symbols.

A matching namespace, extension method, or partial declaration does not make Arcane code part of core. If Arcane requires inaccessible core state, STOP and identify the smallest reusable core hook.

## Localization structure

New Arcane player-visible text requires both `en-US` and `ru-RU` unless explicitly scoped otherwise.

`en-US` is the structural source of truth. When English adds, removes, renames, moves, or reorders a message, attribute, variable, selector, section, or file, apply the same structural change to Russian.

Insert new Russian messages at the corresponding English position. Do not append them to the end unless the English entry is also at the end.

Before selecting a file, inspect both locale trees, search exact and competing keys, identify the existing feature owner file, and inspect its mirrored counterpart.

The established module-local locale namespace directory is `_arcane`. Do not create `arcane` or derive a new hierarchy from the module name.

Preserve exact paths, underscores, casing, attributes, variables, selectors, and ordering. Russian must be natural and must not contain `THE(...)` wrappers.

## Existing infrastructure

Prefer Arcane-local systems, components, prototypes, locale, UI, assets, and the existing `Content.Arcane.IntegrationTests` project.

`Content.Arcane.IntegrationTests` is not a runtime project and MUST NOT be listed in `module.yml`. Do not create another Arcane integration project, duplicate PoolManager setup, or duplicate its CI step.

## Verification

Use the existing Arcane project build and integration-test commands from current workflows. When prototypes, locale, maps, or structured resources change, also run the Release build and YAML linter from root guidance.
