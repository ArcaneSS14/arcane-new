// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Configuration;
using System.Numerics;

namespace Content.Client.UserInterface.Systems.Alerts.Controls;

public sealed partial class AlertControl
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionContainer _motionHost = default!;
    private UiMotionFeedback _motionFeedback = default!;

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
        _motionFeedback = new UiMotionFeedback();
        AddChild(_motionFeedback);
        UpdateMotionProfile();
    }

    private void UpdateMotionProfile(bool updating = false)
    {
        var duration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000);
        _motionHost.Profile = new UiMotionProfile(
            TimeSpan.FromMilliseconds(duration),
            TimeSpan.Zero,
            UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsTravel, 0, 240),
            true)
        {
            EnterEffectDuration = UiMotionSettings.SpatialEffectDuration(duration),
            Fade = !updating,
            Sequence = false
        };
    }

    public void AnimateAppearance(bool updating = false)
    {
        UpdateMotionProfile(updating);
        _motionHost.Show();
    }

    public void AnimateRemoval()
    {
        UpdateMotionProfile();
        _motionHost.CreateExitVisual();
    }
}
// Arcane-End
