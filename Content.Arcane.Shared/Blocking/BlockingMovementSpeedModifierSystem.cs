using Content.Shared.Blocking.Components;
using Content.Shared.Movement.Systems;

namespace Content.Arcane.Shared.Blocking;

public sealed partial class BlockingMovementSpeedModifierSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnRefreshMovementSpeedModifiers(
        Entity<BlockingUserComponent> entity,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        if (entity.Comp.BlockingItem is not { } shield
            || !TryComp<BlockingComponent>(shield, out var blocking)
            || !blocking.IsRaised)
            return;

        args.ModifySpeed(blocking.MovementModifier);
    }
}
