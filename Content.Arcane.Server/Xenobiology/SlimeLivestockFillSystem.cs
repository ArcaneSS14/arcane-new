using Content.Goobstation.Shared.Xenobiology.Systems;

namespace Content.Arcane.Server.Xenobiology;

public sealed partial class SlimeLivestockFillSystem : EntitySystem
{
    [Dependency] private XenobiologySystem _xenobiology = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeLivestockFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SlimeLivestockFillComponent> ent, ref MapInitEvent args)
    {
        for (var i = 0; i < ent.Comp.Count; i++)
        {
            if (_xenobiology.TrySpawnSlimeInContainer(ent, ent.Comp.Container, ent.Comp.Prototype, ent.Comp.Breed))
                continue;

            Log.Error($"Could not fill {ToPrettyString(ent)} with slime {ent.Comp.Breed}.");
            break;
        }
    }
}
