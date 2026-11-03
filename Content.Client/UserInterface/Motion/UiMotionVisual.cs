// Arcane-Start
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Motion;

/// <summary>A short transition on an owner's visual child, never on its input surface.</summary>
public sealed class UiMotionVisual(Control target, IConfigurationManager configuration, CVarDef<bool> group)
{
    private readonly UiMotionTransition _transition = new(configuration);
    private Color _original;
    private Color _applied;
    private Vector2 _scale;
    private Vector2 _appliedScale;
    private float _shrink;
    private float _delay;
    private bool _entering;
    public bool Running { get; private set; }

    public void Start(float milliseconds, bool entering = true, float shrink = 0, float delay = 0)
    {
        var opacity = Running ? _transition.Opacity : entering ? 0 : 1;
        Reset();
        if (!target.VisibleInTree || !UiMotionSettings.IsEnabled(configuration, group) || milliseconds <= 0)
            return;
        _original = _applied = target.Modulate;
        _scale = _appliedScale = target is SpriteView sprite ? sprite.Scale : Vector2.One;
        _shrink = shrink;
        _delay = delay;
        _entering = entering;
        _transition.Start(new UiMotionProfile(TimeSpan.FromMilliseconds(milliseconds),
            TimeSpan.FromMilliseconds(milliseconds), 0, false), entering, opacity, 0);
        Running = true;
        Advance(0);
    }

    public void Advance(float delta)
    {
        if (!Running)
            return;
        if (!target.VisibleInTree || !UiMotionSettings.IsEnabled(configuration, group))
        {
            Reset();
            return;
        }
        if (target.Modulate != _applied)
            _original = target.Modulate;
        if (configuration.GetCVar(UiMotionSettings.Reduce))
            _delay = 0;
        var speed = UiMotionSettings.PlaybackSpeed(configuration);
        var waited = Math.Min(_delay, Math.Max(0, delta) * speed);
        _delay -= waited;
        _transition.Advance(Math.Max(0, delta - waited / speed));
        _applied = _original.WithAlpha(_original.A * _transition.Opacity);
        target.Modulate = _applied;
        if (target is SpriteView sprite)
        {
            if (sprite.Scale != _appliedScale)
                _scale = sprite.Scale;
            _appliedScale = _scale * (1 - (configuration.GetCVar(UiMotionSettings.Reduce) ? 0 : _shrink)
                * MathF.Sin(MathF.PI * _transition.Opacity));
            sprite.Scale = _appliedScale;
        }
        if (!_transition.Finished)
            return;
        if (_entering)
            Reset();
        else
            Running = false;
    }

    public void Reset()
    {
        if (target.Modulate == _applied)
            target.Modulate = _original;
        if (target is SpriteView sprite && sprite.Scale == _appliedScale)
            sprite.Scale = _scale;
        Running = false;
    }
}
// Arcane-End
