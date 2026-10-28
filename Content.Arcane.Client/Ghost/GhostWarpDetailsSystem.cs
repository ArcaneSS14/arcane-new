// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Common.Ghost;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.Ghost;

/// <summary>
/// Passes the server's player details to the ghost warp menu so it can group the list (Orion #107, #121).
/// </summary>
public sealed partial class GhostWarpDetailsSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostWarpDetailsEvent>(OnDetails);
    }

    private void OnDetails(GhostWarpDetailsEvent ev)
    {
        if (_ui.GetActiveUIWidgetOrNull<GhostGui>()?.TargetWindow is not { } window)
            return;

        window.SetDetails(ev);
        window.Populate();
    }
}
