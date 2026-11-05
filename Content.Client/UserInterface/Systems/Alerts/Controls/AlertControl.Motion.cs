// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using System.Numerics;

namespace Content.Client.UserInterface.Systems.Alerts.Controls;

public sealed partial class AlertControl
{
    private const float BaseIconScale = 2;
    private const float AppearanceScale = 0.22f;
    private const float MaximumScale = 1.35f;
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionContainer _motionHost = default!;
    private UiMotionSpring _entranceSpring = new();
    private UiMotionSpring _appearanceScaleSpring = new();
    private UiMotionSpring _severitySpring = new();
    private Vector2 _baseScale;
    private float _entranceElapsed;
    private float _severityPulseElapsed;
    private bool _entranceRunning;
    private bool _severityPulseRunning;

    private void InitializeAlertMotion()
    {
        _motionHost = new UiMotionContainer
        {
            Profile = UiMotionProfile.Standard with { ExitDuration = TimeSpan.Zero },
            EnabledSetting = UiMotionSettings.Alerts,
            ClosingSetting = UiMotionSettings.Alerts,
            ClampToRootBounds = false,
            TravelDirection = -Vector2.UnitX
        };
        _motionHost.AddChild(_icon);
        AddChild(_motionHost);
        _baseScale = _icon.Scale;
    }

    public void AnimateAppearance()
    {
        ResetEntrance();
        if (!AlertSpatialMotionEnabled || UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000) <= 0)
            return;

        _entranceSpring.Snap(0);
        _appearanceScaleSpring.Snap(0);
        _entranceRunning = true;
        var travel = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsTravel, 0, 240);
        ApplyEntranceOffset(new Vector2(travel, 0));
    }

    public void AnimateSeverityChange()
    {
        if (!SeverityPulseEnabled)
        {
            ResetSeverityPulse();
            ApplyIconScale();
            return;
        }

        if (!_severityPulseRunning)
            _severitySpring.Snap(0);
        _severityPulseElapsed = 0;
        _severityPulseRunning = true;
    }

    // Replacement controls keep in-flight springs and velocity before the old control leaves the tree.
    internal void TransferAlertMotionTo(AlertControl other)
    {
        other._entranceSpring = _entranceSpring;
        other._appearanceScaleSpring = _appearanceScaleSpring;
        other._severitySpring = _severitySpring;
        other._entranceElapsed = _entranceElapsed;
        other._severityPulseElapsed = _severityPulseElapsed;
        other._entranceRunning = _entranceRunning;
        other._severityPulseRunning = _severityPulseRunning;
        other.ApplyEntranceOffset(_motionHost.AdditionalVisualOffset);
        other.ApplyIconScale();
        _entranceSpring = new UiMotionSpring();
        _appearanceScaleSpring = new UiMotionSpring();
        _severitySpring = new UiMotionSpring();
        _entranceRunning = false;
        _severityPulseRunning = false;
    }

    private bool AlertSpatialMotionEnabled => UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Alerts)
        && !_motionConfiguration.GetCVar(UiMotionSettings.Reduce);

    private bool ScaleEnabled => AlertSpatialMotionEnabled && _motionConfiguration.GetCVar(UiMotionSettings.AlertsScale);

    private bool SeverityPulseEnabled => ScaleEnabled && _motionConfiguration.GetCVar(UiMotionSettings.AlertsSeverityPulse);

    private void UpdateAlertMotion(float deltaSeconds)
    {
        if (!_entranceRunning && !_severityPulseRunning)
            return;
        if (!AlertSpatialMotionEnabled)
        {
            AnimateRemoval();
            return;
        }

        var delta = Math.Max(0, float.IsFinite(deltaSeconds) ? deltaSeconds : 0)
                    * UiMotionSettings.PlaybackSpeed(_motionConfiguration)
                    * UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsSpeed, 0.5f, 2f);
        var bounce = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsBounce, 0, 1);
        var damping = 1f - bounce * (2f / 3f);
        UpdateEntranceSpring(delta, damping);
        UpdateSeveritySpring(delta, damping);
        ApplyIconScale();
    }

    private void UpdateEntranceSpring(float delta, float damping)
    {
        if (!_entranceRunning)
            return;
        var duration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000) / 1000;
        if (duration <= 0)
        {
            ResetEntrance();
            return;
        }

        var tokenDelta = delta * UiMotionEasing.FastSpatialSeconds / duration;
        var previousElapsed = _entranceElapsed;
        _entranceElapsed += tokenDelta;
        var progress = _entranceSpring.Advance(1, tokenDelta, damping);
        var travel = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsTravel, 0, 240);
        ApplyEntranceOffset(new Vector2(travel * (1 - progress), 0));
        if (ScaleEnabled)
            AdvancePulse(_appearanceScaleSpring, previousElapsed, tokenDelta, damping);
        else
            _appearanceScaleSpring.Snap(0);
        if (_entranceElapsed >= UiMotionEasing.FastSpatialSeconds
            && ((Settled(_entranceSpring, 1) && Settled(_appearanceScaleSpring, 0)) || _entranceElapsed >= 1.2f))
            ResetEntrance();
    }

    private void UpdateSeveritySpring(float delta, float damping)
    {
        if (!_severityPulseRunning)
            return;
        if (!SeverityPulseEnabled)
        {
            ResetSeverityPulse();
            return;
        }

        var previousElapsed = _severityPulseElapsed;
        _severityPulseElapsed += delta;
        AdvancePulse(_severitySpring, previousElapsed, delta, damping);
        if (_severityPulseElapsed >= UiMotionEasing.FastSpatialSeconds
            && (Settled(_severitySpring, 0) || _severityPulseElapsed >= 1.2f))
            ResetSeverityPulse();
    }

    private static bool Settled(UiMotionSpring spring, float target) =>
        MathF.Abs(spring.Value - target) < 0.001f && MathF.Abs(spring.Velocity) < 0.01f;

    private static void AdvancePulse(UiMotionSpring spring, float elapsed, float delta, float damping)
    {
        var rise = Math.Clamp(UiMotionEasing.FastEffectsSeconds - elapsed, 0, delta);
        spring.Advance(1, rise, damping);
        spring.Advance(0, delta - rise, damping);
    }

    // Normalize the spring's first peak so the slider expresses the visible increase, including overshoot.
    private static float PulsePeak(float damping)
    {
        const float frequency = 28.284271f; // sqrt(800), matching UiMotionSpring's default stiffness
        if (damping >= 0.999f)
        {
            var time = UiMotionEasing.FastEffectsSeconds;
            return 1 - MathF.Exp(-frequency * time) * (1 + frequency * time);
        }

        var ratio = MathF.Sqrt(1 - damping * damping);
        var dampedFrequency = frequency * ratio;
        var peakTime = Math.Min(UiMotionEasing.FastEffectsSeconds, MathF.PI / dampedFrequency);
        return 1 - MathF.Exp(-damping * frequency * peakTime)
            * (MathF.Cos(dampedFrequency * peakTime) + damping / ratio * MathF.Sin(dampedFrequency * peakTime));
    }

    private void ApplyIconScale()
    {
        var bounce = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsBounce, 0, 1);
        var peak = PulsePeak(1f - bounce * (2f / 3f));
        var appearance = _entranceRunning && ScaleEnabled ? AppearanceScale * Math.Clamp(_appearanceScaleSpring.Value / peak, 0, 1) : 0;
        var intensity = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsPulseIntensity, 0, 0.35f);
        var severity = _severityPulseRunning && SeverityPulseEnabled ? intensity * Math.Clamp(_severitySpring.Value / peak, 0, 1) : 0;
        var scale = _baseScale * Math.Min(MaximumScale, 1 + Math.Max(appearance, severity));
        if (_icon.Scale != scale)
            _icon.Scale = scale;
    }

    private void ResetEntrance()
    {
        _entranceSpring.Snap(1);
        _appearanceScaleSpring.Snap(0);
        _entranceElapsed = 0;
        _entranceRunning = false;
        ApplyEntranceOffset(Vector2.Zero);
    }

    private void ApplyEntranceOffset(Vector2 offset)
    {
        _motionHost.AdditionalVisualOffset = offset;
        if (_cooldownGraphic is { } cooldown)
            LayoutContainer.SetPosition(cooldown, offset);
    }

    private void ResetSeverityPulse()
    {
        _severitySpring.Snap(0);
        _severityPulseElapsed = 0;
        _severityPulseRunning = false;
    }

    public void AnimateRemoval()
    {
        ResetEntrance();
        ResetSeverityPulse();
        _icon.Scale = _baseScale;
    }

    // SpriteView normally includes its rendered scale in layout; keep the alert's measured size stable.
    private sealed class AlertSpriteView : SpriteView
    {
        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return base.MeasureOverride(availableSize) * new Vector2(BaseIconScale / Scale.X, BaseIconScale / Scale.Y);
        }
    }
}
// Arcane-End
