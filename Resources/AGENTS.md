<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>
SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Base resource guidance

Root `Resources` contains base content and the established owner-specific resource directories. Put Arcane resources in the existing `_Arcane` directories under `Resources` (for example `Resources/Prototypes/_Arcane`, `Resources/Textures/_Arcane`, and `Resources/Locale/{culture}/_Arcane` where present). Use a separate module resource root only when the current owner actually declares one.

Verify prototype schemas, FTL keys, sprite states, audio paths, maps, and licenses. Run YAML or RSI validation as appropriate. Do not duplicate a base resource merely to change one module's behavior when inheritance or a module-local prototype can express it.
