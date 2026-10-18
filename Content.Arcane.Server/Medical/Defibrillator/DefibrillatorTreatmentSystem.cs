using Content.Arcane.Common.Medical.Defibrillator;
using Content.Arcane.Shared.Medical.Heart;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.Defibrillator;

/// <summary>
/// Treats the original patient of a delivered shock before the inherited revival check.
/// </summary>
public sealed partial class DefibrillatorTreatmentSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heart = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private OxygenMedicineSystem _medicine = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, DefibrillatorTreatmentEvent>(OnTreatment);
    }

    private void OnTreatment(Entity<MobStateComponent> ent, ref DefibrillatorTreatmentEvent args)
    {
        if (!args.IsOriginal || !TryComp<DefibrillatorComponent>(args.Defibrillator, out var defib))
            return;

        var wasDead = ent.Comp.CurrentState == MobState.Dead;
        ApplyPulse(ent, args.Defibrillator);
        if (_heart.RequiresHeart(ent))
            args.CanRevive &= _heart.GetHeart(ent) is { } installed &&
                (installed.Comp == null || _heart.GetState(installed.Comp) != HeartState.Stopped);

        var history = EnsureComp<DefibrillatorShockHistoryComponent>(ent);
        var rapid = _timing.CurTime < history.NextSafeShock;
        history.NextSafeShock = _timing.CurTime + TimeSpan.FromTicks(Math.Max(0, defib.ZapDelay.Ticks));
        if (rapid)
        {
            // TODO: Replace thermal injury with heart damage when heart damage affects its function.
            _damage.TryChangeDamage(ent.Owner, new DamageSpecifier { DamageDict = { ["Heat"] = 30 } },
                ignoreResistances: true, origin: args.User, targetPart: TargetBodyPart.Vital,
                splitDamage: SplitDamageBehavior.SplitEnsureAll, interruptsDoAfters: false);
            return;
        }

        if (!wasDead || _rotting.IsRotten(ent) || HasComp<UnrevivableComponent>(ent) ||
            !TryComp<BloodstreamComponent>(ent, out var blood) ||
            !_solutions.ResolveSolution(ent.Owner, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return;

        var heal = _medicine.GetShockHealing(ent, blood.BloodSolution.Value);
        if (heal == 0)
            return;

        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = -heal } },
            ignoreResistances: true, origin: args.User, targetPart: TargetBodyPart.Vital,
            splitDamage: SplitDamageBehavior.SplitEnsureAll, interruptsDoAfters: false);
    }

    private void ApplyPulse(EntityUid patient, EntityUid defibrillator)
    {
        if (!TryComp<HeartDefibrillatorComponent>(defibrillator, out var defib) ||
            _heart.GetHeart(patient) is not { Comp: { } heart } installed)
            return;

        if (_heart.GetState(heart) == HeartState.Stopped)
        {
            var rate = defib.BpmZapHealFlatline;
            if (defib.AutoStabilisation && rate > 0f)
                rate = Math.Clamp(rate, heart.FibrillationCaps.X, heart.FibrillationCaps.Y);
            _heart.SetRate(installed.Owner, heart, rate, true);
        }
        else
            _heart.UpdateRate(installed.Owner, heart, defib.BpmZapHeal, false,
                autoStabilisation: defib.AutoStabilisation);
    }
}
