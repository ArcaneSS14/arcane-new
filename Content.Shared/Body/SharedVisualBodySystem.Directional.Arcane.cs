namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    private void SetEarsAboveHair(Entity<VisualBodyComponent> ent, bool value)
    {
        if (ent.Comp.EarsAboveHair == value)
            return;

        ent.Comp.EarsAboveHair = value;
        Dirty(ent);
    }

    private void CopyDirectionalAppearance(EntityUid source, EntityUid target)
    {
        if (TryComp<VisualBodyComponent>(source, out var sourceVisual) &&
            TryComp<VisualBodyComponent>(target, out var targetVisual))
            SetEarsAboveHair((target, targetVisual), sourceVisual.EarsAboveHair);
    }
}
