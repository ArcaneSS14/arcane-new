using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModPathfinderFlightSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ModPathfinderFlightComponent, GettingPickedUpAttemptEvent>(OnPickup);
        SubscribeLocalEvent<ModPathfinderFlightComponent, ContainerGettingInsertedAttemptEvent>(OnInsert);
    }

    private void OnPickup(Entity<ModPathfinderFlightComponent> ent, ref GettingPickedUpAttemptEvent args)
        => args.Cancel();

    private void OnInsert(Entity<ModPathfinderFlightComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
        => args.Cancel();
}
