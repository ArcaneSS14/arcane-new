using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Restart the server at the end of a round once <see cref="RoundsToRestart"/> rounds have been played.
    /// </summary>
    public static readonly CVarDef<bool> RestartServerOnRoundEnd =
        CVarDef.Create("game.restart_server_on_round_end", false, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// How many rounds the server plays before restarting at a round end.
    /// </summary>
    public static readonly CVarDef<int> RoundsToRestart =
        CVarDef.Create("game.rounds_to_restart", 8, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// NPCs further than this from every player stop thinking.
    /// </summary>
    public static readonly CVarDef<int> NpcSleepRange =
        CVarDef.Create("npc.sleep_range", 40, CVar.SERVERONLY);

    /// <summary>
    /// How many infinity dorms one player can have at once.
    /// </summary>
    public static readonly CVarDef<int> MaxUserInfinityDorms =
        CVarDef.Create("infinity_dorms.max_per_user", 2, CVar.SERVER | CVar.REPLICATED);
}
