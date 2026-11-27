using Content.Server.Research.Systems;
using Content.Shared.Research.Components;

namespace Content.Arcane.Server.Research;

public sealed partial class ResearchServerUnlockAllSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ResearchServerUnlockAllComponent, MapInitEvent>(OnMapInit, after: [typeof(ResearchSystem)]);
    }

    private void OnMapInit(Entity<ResearchServerUnlockAllComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ResearchServerComponent>(ent, out var server))
            return;

        _research.UnlockAllTechnologiesOnServer(ent, server);
    }
}
