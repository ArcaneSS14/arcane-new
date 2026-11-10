using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;

namespace Content.Arcane.Shared.Clothing;

/// <summary>
/// Removes inherited folding from variants without compatible ready-made artwork.
/// Component initialization also runs for editor and nullspace entities without MapInit.
/// </summary>
public sealed partial class FoldedClothingInitializationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoldedClothingVisualsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<FoldedClothingVisualsComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Enabled)
            return;

        // Removal is immediate: no verb or inherited visual survives initialization.
        RemComp<FoldableComponent>(ent);
        RemComp<FoldableClothingComponent>(ent);
        RemComp<FoldedClothingVisualsComponent>(ent);
    }
}
