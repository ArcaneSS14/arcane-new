using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Whether the mood system is enabled.
    /// </summary>
    public static readonly CVarDef<bool> MoodEnabled =
        CVarDef.Create("mood.enabled", true, CVar.SERVER);

    /// <summary>
    ///     Whether good mood increases movement speed.
    /// </summary>
    public static readonly CVarDef<bool> MoodIncreasesSpeed =
        CVarDef.Create("mood.increases_speed", true, CVar.SERVER);

    /// <summary>
    ///     Whether bad mood decreases movement speed.
    /// </summary>
    public static readonly CVarDef<bool> MoodDecreasesSpeed =
        CVarDef.Create("mood.decreases_speed", true, CVar.SERVER);

    /// <summary>
    ///     Whether mood changes the critical and death damage thresholds.
    /// </summary>
    public static readonly CVarDef<bool> MoodModifiesThresholds =
        CVarDef.Create("mood.modify_thresholds", false, CVar.SERVER);

    /// <summary>
    ///     Whether mood desaturates the screen.
    /// </summary>
    public static readonly CVarDef<bool> MoodVisualEffects =
        CVarDef.Create("mood.visual_effects", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
