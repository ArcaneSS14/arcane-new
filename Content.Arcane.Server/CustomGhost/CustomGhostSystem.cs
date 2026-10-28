// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Ghost;
using Content.Arcane.Shared.CustomGhost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.CustomGhost;

/// <summary>
/// Spawns the player's chosen custom ghost instead of the plain observer, if its restrictions allow (Orion #50, #126, #247).
/// </summary>
public sealed partial class CustomGhostSystem : EntitySystem
{
    [Dependency] private INetConfigurationManager _netCfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public const string DefaultGhost = "default";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostPrototypeEvent>(OnGhostPrototype);
    }

    private void OnGhostPrototype(ref GhostPrototypeEvent args)
    {
        if (args.User is not { } userId
            || !_player.TryGetSessionById(userId, out var session)
            || _netCfg.GetClientCVar(session.Channel, ArcaneCVars.CustomGhost) is not { } chosen
            || chosen == DefaultGhost
            || !_proto.TryIndex<CustomGhostPrototype>(chosen, out var ghost)
            || ghost.Abstract
            || !ghost.CanUse(session))
            return;

        args.Prototype = ghost.GhostEntityPrototype;
        args.Custom = true;
    }
}
