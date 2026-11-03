// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Shared.Timing;

namespace Content.Client.ContextMenu.UI;

public sealed partial class ContextMenuUIController
{
    private readonly List<UiMotionSnapshot> _exitVisuals = [];
    private bool _closing;

    internal void AddExitVisual(UiMotionSnapshot? visual)
    {
        if (visual != null)
            _exitVisuals.Add(visual);
    }

    internal void ClearExitVisuals()
    {
        foreach (var visual in _exitVisuals)
            visual.Orphan();
        _exitVisuals.Clear();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        for (var i = _exitVisuals.Count - 1; i >= 0; i--)
        {
            if (!_exitVisuals[i].IsInsideTree)
                _exitVisuals.RemoveAt(i);
        }
    }
}
// Arcane-End
