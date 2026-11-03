// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Motion;

namespace Content.Client.ContextMenu.UI;

public sealed partial class ContextMenuPopup
{
    public override void Open(UIBox2? box = null, Vector2? altPos = null, Vector2? altPosUp = null)
    {
        if (!Visible)
        {
            if (ParentElement == null)
                _uiController.ClearExitVisuals();
            Motion.UseMenuProfile();
            Motion.Show();
        }
        base.Open(box, altPos, altPosUp);
    }

    protected override bool HasPoint(Vector2 point)
    {
        return base.HasPoint(point - Motion.VisualOffset);
    }

    protected override void ModalRemoved()
    {
        // Capture before Popup hides the subtree and menu callbacks clear its contents.
        if (IsInsideTree)
            _uiController.AddExitVisual(Motion.CreateExitVisual());
        base.ModalRemoved();
    }
}
// Arcane-End
