<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>
SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane module guidance

This file is guidance for Arcane code and resources. In this checkout, runtime code is in the root-level `Content.Arcane.*` projects and resources are under existing `_Arcane` directories in root `Resources`. Root hard rules remain in force.

## Owner-local paths and edit markers

Treat `Content.Arcane.*` projects and verified `_Arcane` resource paths as owner-local.

Do not add `// Arcane`, `// Arcane-Edit`, `# Arcane`, `# Arcane-Edit`, or equivalent Arcane markers inside owner-local paths. These markers are only for Arcane changes to inherited files outside Arcane-owned paths.

That includes `Content.Trauma.*`, `Resources/_Trauma/**`, and `*.Trauma.cs` partials. They are owner-local for us, but they are also upstream surface for everyone syncing TraumaStation, so an Arcane change in them is marked with an Arcane marker rather than left bare. Check the current files for established marker patterns.

When a line we change carries an upstream marker such as `# Trauma - was 1800`, the resolved line becomes ours and carries `# Arcane-Edit: 1800 > 3000`. Leaving the upstream marker on our line would claim someone else's authorship and hide our divergence from the next sync. Upstream markers on lines we did not touch stay as they are.

When an inherited file must change, mark the smallest changed block according to `.agents/rules/arcane-edit-markers.md`. Never put a bare `-Start` or `-End` on a line instead of a pair, and do not comment out active code merely to mark a large change.

Before editing a vanilla root path or a Trauma-owned path, look for an owner-local home instead. Syncs arrive along the Trauma trajectory, so both are the conflict surface. See `.agents/rules/fork-trajectory-priority.md` and `.agents/rules/arcane-edit-markers.md`.

## Dependency direction

- Common may depend on root `Content.Common`, but not Arcane Shared, Server, or Client.
- Shared may depend on Arcane Common and root `Content.Shared`.
- Server may depend on Arcane Common, Arcane Shared, and root `Content.Server`.
- Client may depend on Arcane Common, Arcane Shared, and root `Content.Client`.
- Arcane resources belong in the existing `_Arcane` directories under root `Resources`.

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

Prefer Arcane-local systems, components, prototypes, locale, UI, and assets. Use `Content.Tests` or `Content.IntegrationTests` when the existing test ownership covers the behavior.

Do not create a separate Arcane integration-test project or duplicate test fixtures, PoolManager setup, or CI steps without a demonstrated need.

## Verification

Use the existing Arcane project build and integration-test commands from current workflows. When prototypes, locale, maps, or structured resources change, also run the Release build and YAML linter from root guidance.
