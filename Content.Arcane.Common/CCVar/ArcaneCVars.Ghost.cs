using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Custom ghost chosen by the player, a customGhost prototype id. Stored on the client and
    ///     validated by the server against the ghost's restrictions when the ghost spawns.
    /// </summary>
    public static readonly CVarDef<string> CustomGhost =
        CVarDef.Create("ghost.custom_ghost", "default", CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     Seconds a ghost has to wait after death before it can return to the round through the lobby.
    /// </summary>
    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 300f, CVar.SERVERONLY);

    /// <summary>
    ///     Ghosts can only return to the round while fewer players than this are online.
    /// </summary>
    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 80, CVar.SERVERONLY);
}
