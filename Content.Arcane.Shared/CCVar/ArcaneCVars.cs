// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Arcane.Shared.CCVar;

[CVarDefs]
public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Controls validation of saved tile variants when a grid initializes: off, log, on.
    /// </summary>
    public static readonly CVarDef<string> InvalidTileVariantMode =
        CVarDef.Create("tiles.invalid_variant_mode", "on", CVar.SERVERONLY);
}
