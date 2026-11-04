// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using System.Numerics;

namespace Content.Client.UserInterface.Systems.Alerts.Controls;

public sealed partial class AlertControl
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionContainer _motionHost = default!;
    private readonly UiMotionSpring _entranceSpring = new();
    private readonly UiMotionSpring _severitySpring = new();
    private Vector2 _severityBaseScale;
    private float _entranceElapsed;
    private float _severityPulseElapsed;
    private bool _entranceRunning;
    private bool _severityPulseRunning;

    private void InitializeAlertMotion()
    {
        _motionHost = new UiMotionContainer
        {
            EnabledSetting = UiMotionSettings.Alerts,
            ClosingSetting = UiMotionSettings.Alerts,
            ClampToRootBounds = false,
            TravelDirection = -Vector2.UnitX
        };
        _motionHost.AddChild(_icon);
        AddChild(_motionHost);
        _severityBaseScale = _icon.Scale;
    }

    public void AnimateAppearance()
    {
        ResetEntrance();
        if (!AlertSpatialMotionEnabled)
            return;

        var travel = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsTravel, 0, 240);
        var duration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000);
        if (travel <= 0 || duration <= 0)
            return;

        _entranceSpring.Snap(0);
        _entranceElapsed = 0;
        _entranceRunning = true;
        ApplyEntranceOffset(new Vector2(travel, 0));
    }

    public void AnimateSeverityChange()
    {
        if (!SeverityPulseEnabled)
        {
            ResetSeverityPulse();
            return;
        }

        if (!_severityPulseRunning)
        {
            _severityBaseScale = _icon.Scale;
            _severitySpring.Snap(0);
        }
        _severityPulseElapsed = 0;
        _severityPulseRunning = true;
    }

    private bool SeverityPulseEnabled => UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Alerts)
        && _motionConfiguration.GetCVar(UiMotionSettings.AlertsSeverityPulse)
        && !_motionConfiguration.GetCVar(UiMotionSettings.Reduce);

    private bool AlertSpatialMotionEnabled => UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Alerts)
        && !_motionConfiguration.GetCVar(UiMotionSettings.Reduce);

    private void UpdateAlertMotion(float deltaSeconds)
    {
        UpdateEntranceSpring(deltaSeconds);
        UpdateSeveritySpring(deltaSeconds);
    }

    private void UpdateEntranceSpring(float deltaSeconds)
    {
        if (!_entranceRunning)
            return;
        if (!AlertSpatialMotionEnabled)
        {
            ResetEntrance();
            return;
        }

        var configuredDuration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000) / 1000;
        if (configuredDuration <= 0)
        {
            ResetEntrance();
            return;
        }

        var playback = UiMotionSettings.PlaybackSpeed(_motionConfiguration);
        var tokenDelta = Math.Max(0, deltaSeconds) * playback * UiMotionEasing.SlowSpatialSeconds / configuredDuration;
        _entranceElapsed += tokenDelta;
        var progress = _entranceSpring.Advance(1, tokenDelta);
        var travel = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsTravel, 0, 240);
        ApplyEntranceOffset(new Vector2(travel * (1 - progress), 0));
        if (_entranceElapsed >= UiMotionEasing.SlowSpatialSeconds)
            ResetEntrance();
    }

    private void UpdateSeveritySpring(float deltaSeconds)
    {
        if (!_severityPulseRunning)
            return;
        if (!SeverityPulseEnabled)
        {
            ResetSeverityPulse();
            return;
        }

        var delta = Math.Max(0, deltaSeconds) * UiMotionSettings.PlaybackSpeed(_motionConfiguration);
        _severityPulseElapsed += delta;
        var riseDuration = UiMotionEasing.FastEffectsSeconds;
        var target = _severityPulseElapsed < riseDuration ? 1f : 0f;
        var pulse = Math.Clamp(_severitySpring.Advance(target, delta), 0, 1.25f);
        _icon.Scale = _severityBaseScale * (1 + 0.16f * pulse);
        if (_severityPulseElapsed >= UiMotionEasing.FastSpatialSeconds)
            ResetSeverityPulse();
    }

    private void ResetEntrance()
    {
        _entranceSpring.Snap(1);
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
        if (_severityPulseRunning)
            _icon.Scale = _severityBaseScale;
        _severitySpring.Snap(0);
        _severityPulseElapsed = 0;
        _severityPulseRunning = false;
    }

    public void AnimateRemoval()
    {
        ResetEntrance();
        ResetSeverityPulse();
    }
}
// Arcane-End
