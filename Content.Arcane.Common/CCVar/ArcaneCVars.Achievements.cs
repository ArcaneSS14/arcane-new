using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    public static readonly CVarDef<bool> AchievementsEnabled =
        CVarDef.Create("achievements.enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> AchievementsExcludedPresets =
        CVarDef.Create("achievements.excluded_presets", "Sandbox,Deathmatch", CVar.SERVERONLY);

    public static readonly CVarDef<string> AchievementsExcludedMaps =
        CVarDef.Create("achievements.excluded_maps", "Dev,Empty,TestTeg", CVar.SERVERONLY);

    public static readonly CVarDef<float> AchievementsSaveInterval =
        // Invalid intervals, including values outside 1 second to 1 day, use the default.
        CVarDef.Create("achievements.save_interval", 60f, CVar.SERVERONLY);
}
