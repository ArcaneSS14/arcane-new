// Arcane-Start
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>A transient image with no children, focus, modal registration, or input handlers.</summary>
public sealed class UiMotionSnapshot : Control
{
    private readonly UiMotionTransition _transition;
    private readonly Vector2 _origin;
    private readonly Color _tint;
    private readonly ShaderInstance _shader;
    private IRenderTexture? _texture;
    private bool _removalQueued;
    private readonly IConfigurationManager _configuration;
    private readonly CVarDef<bool> _enabled;
    private readonly CVarDef<bool> _closing;

    internal float Opacity => _transition.Opacity;
    internal float Offset => _transition.Offset;

    internal UiMotionSnapshot(IRenderTexture texture, IConfigurationManager configuration,
        UiMotionProfile profile, Vector2 origin, Vector2 size, float opacity, float offset, Color tint, ShaderInstance shader,
        CVarDef<bool> enabled, CVarDef<bool> closing)
    {
        MouseFilter = MouseFilterMode.Ignore;
        _configuration = configuration;
        _enabled = enabled;
        _closing = closing;
        _texture = texture;
        _origin = origin;
        _tint = tint;
        _shader = shader;
        SetSize = size;
        _transition = new UiMotionTransition(configuration);
        _transition.Start(profile, false, opacity, offset);
        UpdateAppearance();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!UiMotionSettings.IsEnabled(_configuration, _enabled) || !_configuration.GetCVar(_closing))
            _transition.Finish();
        _transition.Advance(args.DeltaSeconds);
        UpdateAppearance();
        if (!_transition.Finished || _removalQueued)
            return;

        _removalQueued = true;
        Visible = false;
        UserInterfaceManager.DeferAction(Orphan);
    }

    private void UpdateAppearance()
    {
        var position = _origin + new Vector2(0, _transition.Offset);
        if (Parent != null)
            position = Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, Parent.Size - Size));
        PopupContainer.SetPopupOrigin(this, position);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_texture != null)
            DrawImage(handle, _texture.Texture, PixelSizeBox, _shader, _tint, _transition.Opacity);
    }

    internal static void DrawImage(DrawingHandleScreen handle, Texture texture, UIBox2 box,
        ShaderInstance shader, Color tint, float opacity)
    {
        var previousShader = handle.GetShader();
        var previousModulate = handle.Modulate;
        var alpha = tint.A * opacity;
        // Render targets already contain premultiplied color; fade RGB and alpha together.
        handle.Modulate = new Color(tint.R * alpha, tint.G * alpha, tint.B * alpha, alpha);
        handle.UseShader(shader);
        handle.DrawTextureRect(texture, box);
        handle.UseShader(previousShader);
        handle.Modulate = previousModulate;
    }

    protected override void ExitedTree()
    {
        _texture?.Dispose();
        _texture = null;
        base.ExitedTree();
    }
}
// Arcane-End
