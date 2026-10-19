using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Shared.Bed.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Accumulates capped postmortem suffocation using the common oxygen physiology notifications.
/// </summary>
public sealed partial class OxygenKnockoutSystem
{
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TreatmentGrace = TimeSpan.FromSeconds(10);
    private static readonly FixedPoint2 LowerRate = FixedPoint2.New(1);
    private static readonly FixedPoint2 UpperRate = FixedPoint2.New(0.3);
    private TimeSpan _nextUpdate;

    private bool Eligible(EntityUid uid)
        => TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == MobState.Dead &&
            HasComp<BodyComponent>(uid) && Active<RespiratorComponent>(uid) &&
            !Active<BreathingImmunityComponent>(uid) && !Active<SpecialBreathingImmunityComponent>(uid);

    private void RefreshPostmortem(EntityUid uid)
    {
        if (!Eligible(uid) || _threshold.GetThreshold(uid) is not { } limit)
        {
            RemComp<PostmortemHypoxiaComponent>(uid);
            return;
        }

        var lower = FixedPoint2.New(limit);
        var current = _damage.GetAllDamage(uid).DamageDict.GetValueOrDefault(Asphyxiation);
        if (current >= lower * 2)
        {
            RemComp<PostmortemHypoxiaComponent>(uid);
            return;
        }

        var now = _timing.CurTime;
        if (!TryComp<PostmortemHypoxiaComponent>(uid, out var state))
        {
            state = AddComp<PostmortemHypoxiaComponent>(uid);
            state.NextDamage = now + TreatmentGrace;
            state.LastUpdate = now;
        }
        else if (current < state.Asphyxiation)
            state.NextDamage = now + TreatmentGrace;

        state.Asphyxiation = current;
        state.LowerThreshold = lower;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;
        var query = EntityQueryEnumerator<PostmortemHypoxiaComponent>();
        while (query.MoveNext(out var uid, out var state))
        {
            var elapsed = now - state.LastUpdate;
            state.LastUpdate = now;
            if (HasComp<StasisBedBuckledComponent>(uid))
            {
                state.NextDamage += elapsed;
                continue;
            }

            if (now < state.NextDamage)
                continue;

            state.NextDamage = now + UpdateInterval;
            var lowerStage = state.Asphyxiation < state.LowerThreshold;
            var ceiling = lowerStage ? state.LowerThreshold : state.LowerThreshold * 2;
            var rate = lowerStage ? LowerRate : UpperRate;
            var remaining = ceiling - state.Asphyxiation;
            var amount = rate < remaining ? rate : remaining;
            // Threshold notifications update the cached damage and remove tracking at the cap or on revival.
            _damage.ChangeDamage(uid, new DamageSpecifier { DamageDict = { [Asphyxiation] = amount } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true, interruptsDoAfters: false);
        }
    }
}
