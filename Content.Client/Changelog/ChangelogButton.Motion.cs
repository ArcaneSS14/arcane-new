// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Changelog;

public sealed partial class ChangelogButton
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionVisual? _notificationMotion;
    private bool _hadNewEntries;

    private void AnimateNewEntries(bool hasNewEntries)
    {
        if (hasNewEntries && !_hadNewEntries)
        {
            (_notificationMotion ??= new UiMotionVisual(this, _motionConfiguration, UiMotionSettings.Notifications)).Start(
                UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.NotificationsMs, 0, 1000));
        }

        _hadNewEntries = hasNewEntries;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _notificationMotion?.Advance(args.DeltaSeconds);
    }
}
// Arcane-End
