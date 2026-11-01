// Arcane-Start
using System.Numerics;
using Content.Client.Cooldown;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Content.Shared.CCVar;

namespace Content.Client.Hands;

public sealed partial class ShowHandItemOverlay
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    private UseDelaySystem? _useDelay;
    private readonly CooldownGraphic _arcaneCooldownGraphic = CreateArcaneCooldownGraphic();

    private static CooldownGraphic CreateArcaneCooldownGraphic()
    {
        var graphic = new CooldownGraphic
        {
            SetSize = new Vector2(64f, 64f),
        };

        graphic.Arrange(UIBox2.FromDimensions(Vector2.Zero, graphic.SetSize));
        return graphic;
    }

    private void DrawArcaneCooldown(in OverlayDrawArgs args, EntityUid? handEntity, ScreenCoordinates mousePosition)
    {
        if (!_cfg.GetCVar(CCVars.HudCursorCooldownShow))
        {
            _arcaneCooldownGraphic.Visible = false;
            return;
        }

        if (handEntity is not { } held
            || args.ViewportControl is not Control viewport
            || !_entMan.TryGetComponent(held, out UseDelayComponent? delay))
        {
            _arcaneCooldownGraphic.Visible = false;
            return;
        }

        _useDelay ??= _entMan.System<UseDelaySystem>();
        if (!_useDelay.GetLastActiveDelay((held, delay), out var cooldown))
        {
            _arcaneCooldownGraphic.Visible = false;
            return;
        }

        _arcaneCooldownGraphic.FromTime(cooldown.StartTime, cooldown.EndTime);
        if (!_arcaneCooldownGraphic.Visible)
            return;

        viewport.AddChild(_arcaneCooldownGraphic);
        try
        {
            _ui.RenderControl(args.RenderHandle, _arcaneCooldownGraphic,
                (Vector2i) (mousePosition.Position - _arcaneCooldownGraphic.PixelSize / 2));
        }
        finally
        {
            viewport.RemoveChild(_arcaneCooldownGraphic);
        }
    }

    private void DisposeArcaneCooldown()
    {
        _arcaneCooldownGraphic.Dispose();
    }
}
// Arcane-End
