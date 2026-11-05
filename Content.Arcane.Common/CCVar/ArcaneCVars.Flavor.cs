using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    #region Extended flavor text

    /// <summary>
    ///     Shows the character traits section of the extended flavor text.
    /// </summary>
    public static readonly CVarDef<bool> FlavorTraitsEnabled =
        CVarDef.Create("ic.flavor_traits_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Shows the OOC notes and tags sections of the extended flavor text.
    /// </summary>
    public static readonly CVarDef<bool> FlavorOocEnabled =
        CVarDef.Create("ic.flavor_ooc_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Shows the green/yellow/red roleplay preferences of the extended flavor text.
    /// </summary>
    public static readonly CVarDef<bool> FlavorGyrEnabled =
        CVarDef.Create("ic.flavor_gyr_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Shows the links section of the extended flavor text.
    /// </summary>
    public static readonly CVarDef<bool> FlavorLinksEnabled =
        CVarDef.Create("ic.flavor_links_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Enables the NSFW sections of the extended flavor text. They are still hidden unless
    ///     <see cref="NsfwContentEnabled"/> is set too.
    /// </summary>
    public static readonly CVarDef<bool> FlavorNsfwEnabled =
        CVarDef.Create("ic.flavor_nsfw_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Global switch for NSFW content.
    /// </summary>
    public static readonly CVarDef<bool> NsfwContentEnabled =
        CVarDef.Create("accessibility.nsfw_content_enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorOocLength =
        CVarDef.Create("ic.flavor_ooc_length", 4500, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorCharacterLength =
        CVarDef.Create("ic.flavor_character_length", 4500, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorPreferencesLength =
        CVarDef.Create("ic.flavor_preferences_length", 4500, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorNsfwLength =
        CVarDef.Create("ic.flavor_nsfw_length", 4500, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorTagsLength =
        CVarDef.Create("ic.flavor_tags_length", 256, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorLinksLength =
        CVarDef.Create("ic.flavor_links_length", 512, CVar.SERVER | CVar.REPLICATED);

    #endregion
}
