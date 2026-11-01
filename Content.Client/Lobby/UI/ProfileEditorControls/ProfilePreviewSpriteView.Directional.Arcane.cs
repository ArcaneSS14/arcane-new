using Content.Client.Humanoid.DirectionalLayering;
using Robust.Client.Graphics;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    protected override void Draw(IRenderHandle renderHandle)
    {
        if (Entity is { } entity)
        {
            var layering = EntMan.System<DirectionalLayeringSystem>();
            if (OverrideDirection is { } direction)
                layering.ApplyDummyOrdering(entity.Owner, direction);
            else
            {
                var world = WorldRotation ?? EntMan.System<SharedTransformSystem>().GetWorldRotation(entity.Comp2);
                layering.ApplyDummyOrdering(entity.Owner, world + EyeRotation);
            }
        }

        base.Draw(renderHandle);
    }
}
