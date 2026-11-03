// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed partial class ActionButton
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionVisual[] _motionIcons = [];
    private UiMotionFeedback _motionFeedback = default!;
    private bool? _motionCooling;

    private void InitializeMotion()
    {
        _motionIcons = [new(_bigActionIcon, _motionConfiguration, UiMotionSettings.Actions),
            new(_smallActionIcon, _motionConfiguration, UiMotionSettings.Actions),
            new(_bigItemSpriteView, _motionConfiguration, UiMotionSettings.Actions),
            new(_smallItemSpriteView, _motionConfiguration, UiMotionSettings.Actions)];
        _motionFeedback = new UiMotionFeedback();
        AddChild(_motionFeedback);
    }

    public void AnimateAppearance(float delay)
    {
        foreach (var motion in _motionIcons)
            motion.Start(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.ActionsEnterMs, 0, 1000), delay: delay);
    }

    private void UpdateMotion(float delta)
    {
        foreach (var motion in _motionIcons)
            motion.Advance(delta);
        var cooling = Action?.Comp.Cooldown != null && Cooldown.Progress > 0;
        if (_motionCooling is { } previous && previous != cooling)
            _motionFeedback.Play(UiMotionSettings.Actions, UiMotionSettings.ActionsCooldownPulse, 100);
        _motionCooling = cooling;
    }

    private void ResetMotion()
    {
        foreach (var motion in _motionIcons)
            motion.Reset();
        _motionFeedback?.Stop();
        _motionCooling = null;
    }

    protected override void ExitedTree()
    {
        ResetMotion();
        base.ExitedTree();
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        if (!newVisible)
            ResetMotion();
        base.VisibilityChanged(newVisible);
    }
}
// Arcane-End
