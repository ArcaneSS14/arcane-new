// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.Voting.UI;

public sealed partial class VotePopup
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionContainer _motionHost = default!;

    private void InitializeVoteMotion()
    {
        var surface = new Control();
        while (ChildCount > 0)
        {
            var child = GetChild(0);
            RemoveChild(child);
            surface.AddChild(child);
        }

        _motionHost = new UiMotionContainer
        {
            EnabledSetting = UiMotionSettings.Votes,
            ClosingSetting = UiMotionSettings.Votes,
            TravelDirection = Vector2.UnitX,
        };
        _motionHost.AddChild(surface);
        AddChild(_motionHost);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        var duration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.VotesEnterMs, 0, 1000);
        _motionHost.Profile = new UiMotionProfile(
            TimeSpan.FromMilliseconds(duration),
            TimeSpan.Zero,
            UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.VotesTravel, 0, 240),
            true)
        {
            EnterEffectDuration = UiMotionSettings.SpatialEffectDuration(duration)
        };
        _motionHost.Show();
    }
}
// Arcane-End
