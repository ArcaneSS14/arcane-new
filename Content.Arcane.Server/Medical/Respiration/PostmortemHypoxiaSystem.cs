using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Shared.Bed.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Accumulates capped postmortem suffocation after a grace period without effective oxygen treatment.
/// </summary>
public sealed partial class PostmortemHypoxiaSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private OxygenThresholdSystem _threshold = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TreatmentGrace = TimeSpan.FromSeconds(10);
    private static readonly FixedPoint2 LowerRate = FixedPoint2.New(1);
    private static readonly FixedPoint2 UpperRate = FixedPoint2.New(0.3);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        SubscribeLocalEvent<RespiratorComponent, MobThresholdChecked>(OnThresholds);
        SubscribeLocalEvent<RespiratorComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<RespiratorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RespiratorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentStartup>(OnImmunityStartup);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentShutdown>(OnImmunityShutdown);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentStartup>(OnSpecialStartup);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentShutdown>(OnSpecialShutdown);
    }

    private void OnThresholds(Entity<RespiratorComponent> ent, ref MobThresholdChecked args) => Refresh(ent);
    private void OnStateChanged(Entity<RespiratorComponent> ent, ref MobStateChangedEvent args) => Refresh(ent);
    private void OnStartup(Entity<RespiratorComponent> ent, ref ComponentStartup args) => Refresh(ent);
    private void OnShutdown(Entity<RespiratorComponent> ent, ref ComponentShutdown args) => RemComp<PostmortemHypoxiaComponent>(ent);
    private void OnImmunityStartup(Entity<BreathingImmunityComponent> ent, ref ComponentStartup args) => Refresh(ent);
    private void OnImmunityShutdown(Entity<BreathingImmunityComponent> ent, ref ComponentShutdown args) => Refresh(ent);
    private void OnSpecialStartup(Entity<SpecialBreathingImmunityComponent> ent, ref ComponentStartup args) => Refresh(ent);
    private void OnSpecialShutdown(Entity<SpecialBreathingImmunityComponent> ent, ref ComponentShutdown args) => Refresh(ent);

    private bool Active<T>(EntityUid uid) where T : Component
        => TryComp<T>(uid, out var comp) && comp.LifeStage < ComponentLifeStage.Stopping;

    private bool Eligible(EntityUid uid)
        => HasComp<BodyComponent>(uid) && Active<RespiratorComponent>(uid) &&
            !Active<BreathingImmunityComponent>(uid) && !Active<SpecialBreathingImmunityComponent>(uid) &&
            TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == MobState.Dead;

    private void Refresh(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

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
