// Arcane-Start
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>Inert, one-shot border feedback. Does not dim the information beneath it.</summary>
public sealed class UiMotionFeedback : Control
{
    private readonly IConfigurationManager _configuration = IoCManager.Resolve<IConfigurationManager>();
    private CVarDef<bool>? _group;
    private CVarDef<bool>? _effect;
    private float _remaining;
    private float _duration;
    private bool _pulse;

    public UiMotionFeedback()
    {
        MouseFilter = MouseFilterMode.Ignore;
    }

    public void Play(CVarDef<bool> group, CVarDef<bool> effect, float milliseconds, bool pulse = true)
    {
        _group = group;
        _effect = effect;
        _pulse = pulse;
        _duration = _remaining = Math.Clamp(milliseconds, 0, 1000) / 1000;
        if (!Enabled)
            _remaining = 0;
    }

    public void Stop() => _remaining = 0;

    private bool Enabled => _group != null && _effect != null
        && UiMotionSettings.IsEnabled(_configuration, _group) && _configuration.GetCVar(_effect)
        && (!_pulse || !_configuration.GetCVar(UiMotionSettings.Reduce));

    protected override Vector2 MeasureOverride(Vector2 availableSize) => Vector2.Zero;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _remaining = Enabled ? Math.Max(0, _remaining - args.DeltaSeconds * UiMotionSettings.PlaybackSpeed(_configuration)) : 0;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_remaining <= 0 || !Enabled)
            return;
        var opacity = _remaining / _duration;
        handle.DrawRect(PixelSizeBox, new Color(0.65f, 0.8f, 1f, opacity * 0.6f), false);
    }

    protected override void ExitedTree()
    {
        _remaining = 0;
        base.ExitedTree();
    }
}
// Arcane-End
