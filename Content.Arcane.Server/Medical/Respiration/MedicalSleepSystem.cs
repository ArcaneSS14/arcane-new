using Content.Arcane.Shared.Medical.Respiration;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Coordinates overlapping medical sleep sources without removing unrelated forced or voluntary sleep.
/// </summary>
public sealed partial class MedicalSleepSystem : EntitySystem
{
    [Dependency] private SleepingSystem _sleep = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MobStateSystem _mob = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MedicalSleepStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied,
            before: new[] { typeof(SleepingSystem) });
        SubscribeLocalEvent<MedicalSleepStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<MedicalSleepStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!HasComp<MedicalSleepOwnershipComponent>(args.Target))
        {
            var ownership = EnsureComp<MedicalSleepOwnershipComponent>(args.Target);
            // A sleep already ending on death is not an independent cause after same-tick revival.
            ownership.OwnsSleep = !TryComp<SleepingComponent>(args.Target, out var sleeping) ||
                sleeping.LifeStage >= ComponentLifeStage.Stopping;
            Dirty(args.Target, ownership);
        }
    }

    private void OnRemoved(Entity<MedicalSleepStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) ||
            _status.TryEffectsWithComp<MedicalSleepStatusEffectComponent>(args.Target, out _) ||
            !TryComp<MedicalSleepOwnershipComponent>(args.Target, out var ownership))
            return;

        var wake = ownership.OwnsSleep && !_mob.IsDead(args.Target);
        RemComp<MedicalSleepOwnershipComponent>(args.Target);
        if (wake)
            _sleep.TryWaking(args.Target);
    }
}
