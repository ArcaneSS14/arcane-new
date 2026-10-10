<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Lavaland module guidance

This directory belongs to the Lavaland module. Keep Lavaland-specific code and resources within its declared projects and resource root.

Do not introduce Arcane-only dependencies. When Arcane needs to interact with Lavaland, prefer public contracts or a narrowly defined integration owned by the correct module.

## Mechanics present

The current projects cover megafauna, anger/aggression, weapon upgrades and blocking, procedural chunks/biomes, chasms, tendrils, mobs/fauna, salvage shelter capsules, pressure efficiency, shuttle systems, and boss music. Shared contains most mechanics and selectors; Server owns spawning, map optimization, tendrils, and authoritative behavior; Client contains boss audio, shuttle UI, and visuals; Common carries cross-layer events and components. Project-specific guides link the current source directories and examples. See [`CATALOG.md`](../CATALOG.md) for dependencies and build impact; resources are under root `Resources`.
