using Content.Shared.Forensics.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Forensics.Components;
using Content.Arcane.Shared.Stains;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Stains;

public sealed partial class StainableSystem : SharedStainableSystem
{
    private static readonly ProtoId<TagPrototype> DnaSolutionScannable = "DNASolutionScannable";

    [Dependency] private ForensicsSystem _forensics = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    protected override void DirtyOwnerAppearance(EntityUid item)
    {
        if (_container.TryGetContainingContainer(item, out var container) && TryComp<AppearanceComponent>(container.Owner, out var appearance))
        {
            Dirty(container.Owner, appearance);
        }
    }

    protected override void StainForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution)
    {
        _tag.AddTag(ent.Owner, DnaSolutionScannable);
    }

    protected override void WashingForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution, EntityUid washingMachine)
    {
        if (!TryComp<ForensicsComponent>(washingMachine, out var forensics))
            return;

        forensics.DNAs.UnionWith(_forensics.GetSolutionsDNA(solution.Comp.Solution));
        Dirty(washingMachine, forensics);
    }
}
