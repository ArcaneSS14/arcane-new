// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems.Alerts.Controls;

public sealed partial class AlertControl
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionContainer _motionHost = default!;
    private UiMotionFeedback _motionFeedback = default!;

    private void InitializeAlertMotion()
    {
        _motionHost = new UiMotionContainer { EnabledSetting = UiMotionSettings.Alerts, ClosingSetting = UiMotionSettings.Alerts };
        _motionHost.AddChild(_icon);
        AddChild(_motionHost);
        _motionFeedback = new UiMotionFeedback();
        AddChild(_motionFeedback);
        UpdateMotionProfile();
    }

    private void UpdateMotionProfile()
    {
        _motionHost.Profile = new UiMotionProfile(
            TimeSpan.FromMilliseconds(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsEnterMs, 0, 1000)),
            TimeSpan.FromMilliseconds(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.AlertsExitMs, 0, 1000)),
            0, false) { Sequence = false };
    }

    public void AnimateAppearance()
    {
        UpdateMotionProfile();
        _motionHost.Show();
    }

    public void AnimateRemoval()
    {
        UpdateMotionProfile();
        _motionHost.CreateExitVisual();
    }
}
// Arcane-End
