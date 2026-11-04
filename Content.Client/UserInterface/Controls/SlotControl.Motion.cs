// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

public abstract partial class SlotControl
{
    [Dependency] private IConfigurationManager _motionConfiguration = default!;
    private UiMotionVisual? _highlightMotion;
    private bool _motionHighlight;

    private void SetMotionHighlight(bool value)
    {
        if (_motionHighlight == value)
            return;
        _motionHighlight = value;
        _highlightMotion ??= new UiMotionVisual(HighlightRect, _motionConfiguration, UiMotionSettings.SlotHighlight);
        HighlightRect.Visible = true;
        if (UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots))
            _highlightMotion.Start(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotEnterMs, 0, 1000), value);
        else
            _highlightMotion.Reset();
        if (!_highlightMotion.Running)
            HighlightRect.Visible = value;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _highlightMotion?.Advance(args.DeltaSeconds);
        if (!UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots))
            _highlightMotion?.Reset();
        if (_highlightMotion is { Running: false })
            HighlightRect.Visible = _motionHighlight;
    }

    protected override void ExitedTree()
    {
        ClearSlotMotion();
        base.ExitedTree();
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        if (!newVisible)
            ClearSlotMotion();
        base.VisibilityChanged(newVisible);
    }

    private void ClearSlotMotion()
    {
        _highlightMotion?.Reset();
        HighlightRect.Visible = _motionHighlight;
    }
}
// Arcane-End
