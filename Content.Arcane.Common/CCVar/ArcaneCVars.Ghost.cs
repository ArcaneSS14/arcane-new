// SPDX-License-Identifier: AGPL-3.0-or-later

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
}
