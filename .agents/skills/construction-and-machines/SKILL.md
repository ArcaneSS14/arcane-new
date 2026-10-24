---
name: construction-and-machines
description: Implement construction graphs, machine lifecycle, power, parts, upgrades, examination, and machine UI.
---

Machines often combine prototypes, construction, anchoring, power, components, server simulation, UI, and upgrade parts.

## Construction

Find the current graph and node conventions. Keep steps data-driven. Validate tools, materials, entity replacement, deconstruction, and state transfer. Avoid source-fork graph IDs that do not exist in the destination.

## Machine lifecycle

Define behavior for map initialization, anchoring, unanchoring, power changes, damage, breakage, deconstruction, and deletion. Systems should subscribe to lifecycle events instead of polling every machine each tick.

## Parts and upgrades

Use existing part-rating or machine-board APIs. Recalculate derived values from installed parts at the established event point. Keep prototype defaults valid when no upgraded parts are present. Do not cache a derived value without invalidation.

## UI and authority

The server owns machine state, inventory, costs, output, and access. BUI messages express intent and are revalidated. UI state should expose only values the user needs.

## Common failures

- upgraded value never recalculated after part change;
- construction replacement loses ownership or contents;
- machine violates its specified power or anchoring requirement;
- server trusts client-selected output or price;
- map-spawned machines skip setup;
- examine text disagrees with actual state.

## Verification

Select only cases exercising the changed contract under [verification](../../rules/verification.md); the following list is conditional, not a required full suite.

Test construction, deconstruction, map spawn, power loss, anchoring, part replacement, damage, UI concurrency, and YAML references.

For each changed transition in prototype/map spawn → initialization → anchoring/power → interaction/part change → damage → deconstruction/deletion, identify derived values and UI needing event-driven invalidation.
