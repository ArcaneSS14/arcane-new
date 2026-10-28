// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

[CVarDefs]
public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Controls validation of saved tile variants when a grid initializes: off, log, on.
    /// </summary>
    public static readonly CVarDef<string> InvalidTileVariantMode =
        CVarDef.Create("tiles.invalid_variant_mode", "on", CVar.SERVERONLY);

    #region Sprinting

    /// <summary>
    ///     Is sprinting enabled
    /// </summary>
    public static readonly CVarDef<bool> SprintEnabled =
        CVarDef.Create("sprint.sprint_enabled", true, CVar.SERVER | CVar.REPLICATED);

    #endregion

    /// <summary>
    ///     Whether space wind spawns subtle visual indicators of air currents.
    /// </summary>
    public static readonly CVarDef<bool> SpaceWindVisuals =
        CVarDef.Create("atmos.space_wind_visuals", true, CVar.SERVERONLY);

    #region Directional emotes

    /// <summary>
    ///     Enables directional emotes, seen only by the sender and the target.
    /// </summary>
    public static readonly CVarDef<bool> DirectionalEmotesEnabled =
        CVarDef.Create("chat.directional_emotes_enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum distance to the target of a directional emote.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteRange =
        CVarDef.Create("chat.directional_emotes_range", 3, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum length of a directional emote.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteMaxLength =
        CVarDef.Create("chat.directional_emotes_max_length", 10000, CVar.SERVER | CVar.REPLICATED);

    #endregion
}
