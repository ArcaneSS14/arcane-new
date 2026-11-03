// Arcane-Start
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>Short-lived visual copy. Contains no input handlers or original control references.</summary>
public sealed class UiMotionGhost : Control
{
    private readonly UiMotionVisual _fade;
    private readonly EntityUid? _entity;
    private bool _queued;
    private bool _started;

    public UiMotionGhost(Control visual, CVarDef<bool> group, float milliseconds, Vector2 position, Vector2 size, EntityUid? entity = null)
    {
        MouseFilter = visual.MouseFilter = MouseFilterMode.Ignore;
        SetSize = size;
        PopupContainer.SetPopupOrigin(this, position);
        AddChild(visual);
        _entity = entity;
        _fade = new UiMotionVisual(visual, IoCManager.Resolve<IConfigurationManager>(), group);
        OnVisibilityChanged += _ => { if (!VisibleInTree) _fade.Reset(); };
        _milliseconds = milliseconds;
    }

    private readonly float _milliseconds;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!_started)
        {
            _started = true;
            _fade.Start(_milliseconds, false);
        }
        _fade.Advance(args.DeltaSeconds);
        if (_queued || (_fade.Running && (_entity == null || IoCManager.Resolve<IEntityManager>().EntityExists(_entity))))
            return;
        _queued = true;
        Visible = false;
        UserInterfaceManager.DeferAction(Orphan);
    }

    protected override void ExitedTree()
    {
        _fade.Reset();
        base.ExitedTree();
    }
}
// Arcane-End
