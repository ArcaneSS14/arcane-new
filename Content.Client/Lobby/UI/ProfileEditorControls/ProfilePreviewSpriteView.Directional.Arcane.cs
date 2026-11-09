using Robust.Client.Graphics; // Arcane

namespace Content.Client.Lobby.UI.ProfileEditorControls; // Arcane

// Arcane-Start
public sealed partial class ProfilePreviewSpriteView
{
    protected override void Draw(IRenderHandle renderHandle)
    {
        if (Entity is { } entity)
        {
            var rotation = OverrideDirection == null
                ? (WorldRotation ?? EntMan.System<SharedTransformSystem>().GetWorldRotation(entity.Comp2)) + EyeRotation
                : Angle.Zero;
            var ev = new ProfilePreviewSpriteDrawingEvent(OverrideDirection, rotation);
            EntMan.EventBus.RaiseLocalEvent(entity.Owner, ref ev);
        }

        base.Draw(renderHandle);
    }
}
// Arcane-End
