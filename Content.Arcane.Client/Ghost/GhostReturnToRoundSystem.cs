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
    private int? _lastShown;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_ui.GetActiveUIWidgetOrNull<GhostGui>() is not { } gui)
            return;

        if (_hooked != gui)
        {
            _hooked = gui;
            _lastShown = null;
            gui.ReturnToRoundButton.OnPressed += _ => RaiseNetworkEvent(new GhostReturnToRoundRequestEvent());
        }

        if (!TryComp<GhostRespawnComponent>(_player.LocalEntity, out var respawn))
        {
            gui.ReturnToRoundButton.Visible = false;
            return;
        }

        var left = respawn.RespawnAt - _timing.CurTime;
        var ready = left <= TimeSpan.Zero;

        // The label only changes with the shown second, so this runs before any formatting.
        var shown = ready ? -1 : (int) left.TotalSeconds;
        gui.ReturnToRoundButton.Visible = true;
        if (shown == _lastShown)
            return;

        _lastShown = shown;
        gui.ReturnToRoundButton.Text = ready
            ? Loc.GetString("ghost-gui-return-to-round-ready-button")
            : Loc.GetString("ghost-gui-return-to-round-button", ("time", $"{(int) left.TotalMinutes:00}:{left.Seconds:00}"));
        gui.ReturnToRoundButton.Disabled = !ready;
    }
}
