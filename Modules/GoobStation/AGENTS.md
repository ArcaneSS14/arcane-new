<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# GoobStation module guidance

This directory belongs to the GoobStation module inherited by Arcane.

Modify it for a Goob-owned feature, a reusable inherited fix, or an extension point that genuinely belongs here. Do not place Arcane-only logic here to avoid creating the corresponding Arcane implementation.

Keep diffs narrow and easy to compare with upstream Goob Reforged.

This module is not part of the Trauma trajectory. Syncs do not flow through it, so an edit here does not collide with the next Trauma import. That makes it a legitimate place for inherited fixes that would be expensive in a vanilla root path, which is the conflict surface. Trajectory classification: `.agents/rules/fork-trajectory-priority.md`.

## Mechanics present

The current C# source is concentrated in Shared and Server. Major areas include Wraith, Shadowling, Changeling, Blob, Devil, Xenobiology, disease, enchanting, religion, implants, station events, and related antagonist/objective systems. Client contains corresponding visuals and UI, plus research and console interfaces. Common carries events and contracts; UIKit owns reusable Goob UI controls and rich text. Per-project guides under this directory link source areas and representative files; [`CATALOG.md`](../CATALOG.md) maps their dependencies and builds. Prototypes and assets are in root `Resources`.
