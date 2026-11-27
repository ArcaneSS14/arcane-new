using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Start a map vote and select a weighted random preset at the start of the lobby.
    /// </summary>
    public static readonly CVarDef<bool> AutoVotingEnabled =
        CVarDef.Create("vote.auto_voting_enabled", false, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// How many past rounds' presets are remembered to lower their weight in the random preset pick.
    /// </summary>
    public static readonly CVarDef<int> GamePresetHistorySize =
        CVarDef.Create("game.preset_history_size", 8, CVar.SERVERONLY | CVar.ARCHIVE);
}
