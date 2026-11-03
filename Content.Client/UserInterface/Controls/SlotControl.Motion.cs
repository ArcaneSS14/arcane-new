// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

public abstract partial class SlotControl
{
    [Dependency] private IConfigurationManager _motionConfiguration = default!;
    private UiMotionVisual? _iconMotion;
    private UiMotionVisual? _exitMotion;
    private UiMotionVisual? _highlightMotion;
    private SpriteView? _exitIcon;
    private EntProtoId? _motionPrototype;
    private bool _prototypeFaded;
    private bool _motionHighlight;
    private SpriteView? _pendingAppearance;

    private bool BeginSlotChange(EntityUid? entity, EntProtoId? prototype, bool faded)
    {
        if (Entity == entity && _motionPrototype == prototype && _prototypeFaded == faded)
            return false;

        ClearExitIcon();
        _iconMotion?.Reset();
        if (VisibleInTree && UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots)
            && UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotExitMs, 0, 1000) > 0)
        {
            if (ProtoView.Visible && _motionPrototype is { } oldPrototype)
            {
                var view = new EntityPrototypeView();
                view.SetPrototype(oldPrototype);
                _exitIcon = view;
                if (_prototypeFaded)
                    view.Modulate = Color.DarkGray.WithAlpha(0.65f);
            }
            else if (SpriteView.Visible && Entity is { } oldEntity
                     && IoCManager.Resolve<IEntityManager>().EntityExists(oldEntity))
            {
                _exitIcon = new SpriteView();
                _exitIcon.SetEntity(oldEntity);
            }
            if (_exitIcon != null)
            {
                _exitIcon.Scale = new Vector2(2);
                _exitIcon.SetSize = new Vector2(DefaultButtonSize);
                _exitIcon.OverrideDirection = Direction.South;
                _exitIcon.MouseFilter = MouseFilterMode.Ignore;
                AddChild(_exitIcon);
                _exitIcon.SetPositionInParent(SpriteView.GetPositionInParent() + 1);
                _exitMotion = new UiMotionVisual(_exitIcon, _motionConfiguration, UiMotionSettings.Slots);
                _exitMotion.Start(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotExitMs, 0, 1000), false);
            }
        }
        _motionPrototype = prototype;
        _prototypeFaded = faded;
        return true;
    }

    private void AnimateSlotIcon(SpriteView view)
    {
        if (!view.IsInsideTree)
        {
            _pendingAppearance = view;
            return;
        }
        _pendingAppearance = null;
        _iconMotion = new UiMotionVisual(view, _motionConfiguration, UiMotionSettings.Slots);
        _iconMotion.Start(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotEnterMs, 0, 1000),
            shrink: UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotScale, 0, 0.25f));
    }

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
        if (_pendingAppearance is { VisibleInTree: true } view)
            AnimateSlotIcon(view);
        _iconMotion?.Advance(args.DeltaSeconds);
        _exitMotion?.Advance(args.DeltaSeconds);
        _highlightMotion?.Advance(args.DeltaSeconds);
        if (!UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots))
            _highlightMotion?.Reset();
        if (_highlightMotion is { Running: false })
            HighlightRect.Visible = _motionHighlight;
        if (_exitIcon != null && (_exitMotion is not { Running: true }
            || !IoCManager.Resolve<IEntityManager>().EntityExists(_exitIcon.Entity?.Owner)))
            ClearExitIcon();
    }

    private void ClearExitIcon()
    {
        _exitMotion?.Reset();
        _exitMotion = null;
        _exitIcon?.Orphan();
        _exitIcon = null;
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
        _pendingAppearance = null;
        _iconMotion?.Reset();
        _highlightMotion?.Reset();
        HighlightRect.Visible = _motionHighlight;
        ClearExitIcon();
    }
}
// Arcane-End
