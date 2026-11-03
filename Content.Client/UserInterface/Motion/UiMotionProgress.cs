// Arcane-Start
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>A bounded visual interpolation; logical values remain immediately available to the owner.</summary>
public sealed class UiMotionProgressValue
{
    public float Displayed { get; private set; }
    private float _start;
    private float _target;
    private float _age;
    private bool _initialized;

    public float Advance(float target, float delta, float duration, bool enabled)
    {
        target = float.IsFinite(target) ? Math.Clamp(target, 0, 1) : 0;
        if (!_initialized || !enabled || duration <= 0 || !float.IsFinite(duration) || target is 0 or 1)
        {
            _initialized = true;
            _start = _target = Displayed = target;
            _age = 0;
            return Displayed;
        }
        if (_target != target)
        {
            _start = Displayed;
            _target = target;
            _age = 0;
        }
        _age += Math.Max(0, float.IsFinite(delta) ? delta : 0);
        Displayed = _start + (_target - _start) * Math.Clamp(_age / duration, 0, 1);
        return Displayed;
    }
}

/// <summary>ProgressBar presentation with a separate immediate target value.</summary>
public sealed class UiMotionProgressBar : Control
{
    private readonly IConfigurationManager _configuration = IoCManager.Resolve<IConfigurationManager>();
    private readonly ProgressBar _bar = new();
    private readonly UiMotionProgressValue _value = new();
    public float Value { get; set; }
    public float MaxValue { get => _bar.MaxValue; set => _bar.MaxValue = value; }
    public StyleBox? BackgroundStyleBoxOverride { get => _bar.BackgroundStyleBoxOverride; set => _bar.BackgroundStyleBoxOverride = value; }
    public StyleBox? ForegroundStyleBoxOverride { get => _bar.ForegroundStyleBoxOverride; set => _bar.ForegroundStyleBoxOverride = value; }

    public UiMotionProgressBar() => AddChild(_bar);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var ratio = MaxValue > 0 ? Value / MaxValue : 0;
        _bar.Value = _value.Advance(ratio, args.DeltaSeconds * UiMotionSettings.PlaybackSpeed(_configuration),
            UiMotionSettings.Read(_configuration, UiMotionSettings.ProgressMs, 0, 1000) / 1000,
            UiMotionSettings.IsEnabled(_configuration, UiMotionSettings.Progress) && !_configuration.GetCVar(UiMotionSettings.Reduce)) * MaxValue;
    }
}
// Arcane-End
