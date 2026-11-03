// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Administration.UI.Bwoink;

public sealed partial class BwoinkPanel
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionVisual? _channelMotion;
    private UiMotionFeedback? _messageFeedback;

    public void AnimateChannelAppearance()
    {
        (_channelMotion ??= new UiMotionVisual(this, _motionConfiguration, UiMotionSettings.Tabs)).Start(
            UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.TabsEnterMs, 0, 1000));
    }

    private void AnimateIncomingMessage()
    {
        _messageFeedback ??= new UiMotionFeedback();
        if (_messageFeedback.Parent == null)
            AddChild(_messageFeedback);
        _messageFeedback.Play(UiMotionSettings.Notifications, UiMotionSettings.Notifications,
            UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.NotificationsMs, 0, 1000));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _channelMotion?.Advance(args.DeltaSeconds);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        _messageFeedback?.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
        return size;
    }
}
// Arcane-End
