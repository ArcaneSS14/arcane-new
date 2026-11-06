using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Counts the spins and somersaults of <see cref="CapoeiraFlowComponent"/>, clients animate them.
/// </summary>
public sealed partial class CapoeiraFlowSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<CapoeiraFlowComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || _standing.IsDown(args.User))
            return;

        var comp = EnsureComp<CapoeiraFlowUserComponent>(args.User);
        comp.Flips++;
        comp.LastFlip = _timing.CurTime;
        Dirty(args.User, comp);
    }

    [SubscribeLocalEvent]
    private void OnComboAttack(Entity<CapoeiraFlowComponent> ent, ref ComboAttackPerformedEvent args)
    {
        // shoving yourself is how you kick up, not an attack
        if (args.Type != ComboAttackType.Disarm || args.Performer == args.Target || _standing.IsDown(args.Performer))
            return;

        var comp = EnsureComp<CapoeiraFlowUserComponent>(args.Performer);
        comp.Spins++;
        comp.LastSpin = _timing.CurTime;
        Dirty(args.Performer, comp);
    }
}
