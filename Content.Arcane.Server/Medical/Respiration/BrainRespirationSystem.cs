using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Metabolism;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Body;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.Respiration;

public sealed partial class BrainRespirationSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BrainOxygenSystem _oxygen = default!;
    [Dependency] private LungSystem _lungs = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private StatusEffectsSystem _effects = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly EntProtoId OxygenEffect = "StatusEffectOxygenDeprived";
    private static readonly EntProtoId SpeechEffect = "StatusEffectHypoxicSpeech";
    private static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(4);
    private bool _enabled;
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(ArcaneCVars.ComplexRespiratorEnabled, OnEnabledChanged, true);
        SubscribeLocalEvent<LungOxygenComponent, BodyRelayedEvent<InhaledGasEvent>>(OnInhaled,
            before: new[] { typeof(RespiratorSystem) });
        SubscribeLocalEvent<BodyComponent, SuffocationBeforeEvent>(OnBeforeSuffocation);
        SubscribeLocalEvent<DamageableComponent, CPRCompletedEvent>(OnCPRCompleted);
        SubscribeLocalEvent<BrainOxygenComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<BrainOxygenComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BrainOxygenComponent, ComponentShutdown>(OnBrainShutdown);
        SubscribeLocalEvent<BrainOxygenComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(ArcaneCVars.ComplexRespiratorEnabled, OnEnabledChanged);
        base.Shutdown();
    }

    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;
        _nextUpdate = _timing.CurTime;
        if (enabled)
            return;

        var query = EntityQueryEnumerator<BrainOxygenComponent>();
        while (query.MoveNext(out var uid, out var brain))
        {
            SetOwner((uid, brain), null);
            StoreSaturation((uid, brain), 1f);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_enabled || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;
        var query = EntityQueryEnumerator<BrainOxygenComponent>();
        while (query.MoveNext(out var uid, out var brain))
        {
            var body = _body.GetBody(uid);
            SetOwner((uid, brain), body);
            if (body is not null && !brain.Active)
                continue;

            StoreSaturation((uid, brain), brain.AirSaturation - brain.AirConsumption);
            if (brain.LastBody is not { } patient)
                continue;

            RefreshEffects((uid, brain));
            if (BrainOxygenSystem.GetLevel(brain) >= BrainOxygenLevel.Critical && !_mob.IsDead(patient))
                ChangeAsphyxiation(patient, brain.AsphyxiationDamage);

            if (_mob.IsHardCrit(patient) && TryComp<RespiratorComponent>(patient, out var respirator))
                _respirator.TryGasp((patient, respirator));
        }
    }

    private void OnInhaled(Entity<LungOxygenComponent> ent, ref BodyRelayedEvent<InhaledGasEvent> args)
    {
        if (!_enabled || args.Args.Handled || !HasComp<MetabolizerComponent>(ent) ||
            _oxygen.GetBrain(args.Body.Owner) is not { } brain || IsBreathingBlocked(args.Body.Owner))
            return;

        // Read the incoming mixture before the normal handler transfers it into the lungs.
        var saturation = _respirator.GetSaturation(_lungs.GasToReagent(args.Args.Gas), ent.Owner, out _);
        SetSaturation(brain, brain.Comp.AirSaturation + saturation * ent.Comp.AirSaturationGain);
    }

    private bool IsBreathingBlocked(EntityUid body)
    {
        return HasComp<BlockedBreathingComponent>(body) ||
            TryComp<PullableComponent>(body, out var pullable) && pullable.GrabStage == GrabStage.Suffocate;
    }

    private void OnBeforeSuffocation(Entity<BodyComponent> ent, ref SuffocationBeforeEvent args)
    {
        if (_oxygen.GetBrain(ent) is not null)
            args.Cancelled = true;
    }

    private void OnCPRCompleted(Entity<DamageableComponent> ent, ref CPRCompletedEvent args)
    {
        if (_enabled)
            ChangeAsphyxiation(ent, -(_oxygen.GetBrain(ent)?.Comp.CPRRecovery ?? 3f));
    }

    private void ChangeAsphyxiation(EntityUid body, float amount)
    {
        _damage.ChangeDamage(body,
            new DamageSpecifier { DamageDict = { ["Asphyxiation"] = amount } },
            targetPart: TargetBodyPart.Vital, interruptsDoAfters: false, ignoreResistances: true);
    }

    public void SetSaturation(Entity<BrainOxygenComponent> brain, float saturation)
    {
        StoreSaturation(brain, saturation);
        RefreshEffects(brain);
    }

    private void StoreSaturation(Entity<BrainOxygenComponent> brain, float saturation)
    {
        saturation = Math.Clamp(saturation, 0f, 1f);
        if (brain.Comp.AirSaturation == saturation)
            return;

        brain.Comp.AirSaturation = saturation;
        Dirty(brain);
    }

    private void OnInserted(Entity<BrainOxygenComponent> ent, ref OrganGotInsertedEvent args)
    {
        SetOwner(ent, args.Target);
        RefreshEffects(ent);
    }

    private void OnRemoved(Entity<BrainOxygenComponent> ent, ref OrganGotRemovedEvent args)
    {
        SetOwner(ent, null);
    }

    private void OnBrainShutdown(Entity<BrainOxygenComponent> ent, ref ComponentShutdown args)
    {
        ClearBody(ent.Comp.LastBody);
    }

    private void OnRejuvenate(Entity<BrainOxygenComponent> ent, ref RejuvenateEvent args)
    {
        SetSaturation(ent, 1f);
    }

    private void SetOwner(Entity<BrainOxygenComponent> brain, EntityUid? body)
    {
        if (!_enabled || body is { } uid && (!HasComp<RespiratorComponent>(uid) ||
            HasComp<BreathingImmunityComponent>(uid) || HasComp<SpecialBreathingImmunityComponent>(uid)))
            body = null;

        if (brain.Comp.LastBody != body)
        {
            ClearBody(brain.Comp.LastBody);
            brain.Comp.LastBody = body;
            brain.Comp.LastLevel = null;
            brain.Comp.HadVisualEffect = false;

            if (body is { } patient && _body.GetOrgan(patient, LungsCategory) is { } lungs &&
                TryComp<LungComponent>(lungs, out var lung))
                _alerts.ClearAlert(patient, lung.Alert);
        }

        var active = body is not null;
        if (brain.Comp.Active == active)
            return;

        brain.Comp.Active = active;
        Dirty(brain);
    }

    private void RefreshEffects(Entity<BrainOxygenComponent> brain)
    {
        if (!_enabled || brain.Comp.LastBody is not { } body || TerminatingOrDeleted(body))
            return;

        var level = BrainOxygenSystem.GetLevel(brain.Comp);
        if (brain.Comp.LastLevel != level)
        {
            ClearAlerts(body);
            var alert = level switch
            {
                BrainOxygenLevel.Unstable => "BrainOxygenUnstable",
                BrainOxygenLevel.Dangerous => "BrainOxygenDangerous",
                BrainOxygenLevel.Critical or BrainOxygenLevel.Fatal => "BrainOxygenCritical",
                _ => null,
            };
            if (alert is not null)
                _alerts.ShowAlert(body, alert);
            brain.Comp.LastLevel = level;
        }

        var intensity = Math.Clamp((0.9f - brain.Comp.AirSaturation) / 0.55f * 0.65f, 0f, 0.65f);
        if (intensity > 0)
        {
            _effects.TrySetStatusEffectDuration(body, OxygenEffect, TimeSpan.FromMinutes(45) * intensity);
            brain.Comp.HadVisualEffect = true;
        }
        else if (brain.Comp.HadVisualEffect)
        {
            _effects.TrySetStatusEffectDuration(body, OxygenEffect, TimeSpan.FromSeconds(5));
            brain.Comp.HadVisualEffect = false;
        }

        if (level >= BrainOxygenLevel.Critical)
            _effects.TrySetStatusEffectDuration(body, SpeechEffect, TimeSpan.FromSeconds(5));
        else
            _effects.TryRemoveStatusEffect(body, SpeechEffect);
    }

    private void ClearBody(EntityUid? body)
    {
        if (body is not { } patient || TerminatingOrDeleted(patient))
            return;

        ClearAlerts(patient);
        _effects.TryRemoveStatusEffect(patient, OxygenEffect);
        _effects.TryRemoveStatusEffect(patient, SpeechEffect);
    }

    private void ClearAlerts(EntityUid body)
    {
        _alerts.ClearAlert(body, "BrainOxygenUnstable");
        _alerts.ClearAlert(body, "BrainOxygenDangerous");
        _alerts.ClearAlert(body, "BrainOxygenCritical");
    }
}
