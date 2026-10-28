// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.Ghost;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Ghost;

/// <summary>
/// Drives the ghost GUI "return to round" button (Orion #54, #253, #261, #389, #396).
/// </summary>
public sealed partial class GhostReturnToRoundSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private GhostGui? _hooked;
    private string? _lastText;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_ui.GetActiveUIWidgetOrNull<GhostGui>() is not { } gui)
            return;

        if (_hooked != gui)
        {
            _hooked = gui;
            _lastText = null;
            gui.ReturnToRoundButton.OnPressed += _ => RaiseNetworkEvent(new GhostReturnToRoundRequestEvent());
        }

        if (!TryComp<GhostRespawnComponent>(_player.LocalEntity, out var respawn))
        {
            gui.ReturnToRoundButton.Visible = false;
            return;
        }

        var left = respawn.RespawnAt - _timing.CurTime;
        var ready = left <= TimeSpan.Zero;
        var text = ready
            ? Loc.GetString("ghost-gui-return-to-round-ready-button")
            : Loc.GetString("ghost-gui-return-to-round-button", ("time", $"{(int) left.TotalMinutes:00}:{left.Seconds:00}"));

        gui.ReturnToRoundButton.Visible = true;
        if (text == _lastText)
            return;

        _lastText = text;
        gui.ReturnToRoundButton.Text = text;
        gui.ReturnToRoundButton.Disabled = !ready;
    }
}
