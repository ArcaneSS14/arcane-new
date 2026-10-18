using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Medical.Common.Traumas;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Supplies physiology-specific CPR outcomes while the inherited system owns the interaction and session.
/// </summary>
public sealed partial class CPRRecoverySystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private HeartRateSystem _heart = default!;
    [Dependency] private OxygenThresholdSystem _oxygen = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Lungs = "Lungs";

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, CPRCapabilitiesEvent>(OnCapabilities);
        SubscribeLocalEvent<DamageableComponent, CPRCompletedEvent>(OnCompleted);
        SubscribeLocalEvent<MobStateComponent, CPRRevivedEvent>(OnRevived);
        SubscribeLocalEvent<CPRPerfusionComponent, CPRStoppedEvent>(OnStopped);
    }

    private void OnCapabilities(Entity<MobStateComponent> ent, ref CPRCapabilitiesEvent args)
    {
        args.Handled = true;
        args.CanPerform = ent.Owner != args.User && ent.Comp.CurrentState != MobState.Alive &&
            HasComp<RespiratorComponent>(ent) && _body.GetOrgan(ent, Lungs) != null;
        args.CanVentilate = args.CanPerform &&
            TryComp<RespiratorComponent>(args.User, out var respirator) &&
            !HasComp<BreathingImmunityComponent>(args.User) && !HasComp<SpecialBreathingImmunityComponent>(args.User) &&
            !HasComp<BreathingImmunityComponent>(ent) && !HasComp<SpecialBreathingImmunityComponent>(ent) &&
            _respirator.CanBreathe(args.User, respirator) && HealthyLungs(args.User) && HealthyLungs(ent) &&
            MouthAvailable(args.User, args.User) && MouthAvailable(args.User, ent);
        args.VerbText = Loc.GetString(args.CanVentilate ? "cpr-verb" : "arcane-chest-compressions-verb");
    }

    private bool HealthyLungs(EntityUid patient)
        => _body.GetOrgan(patient, Lungs) is { } lungs &&
            TryComp<InternalChildOrganComponent>(lungs, out var organ) && organ.OrganSeverity == OrganSeverity.Normal;

    private bool MouthAvailable(EntityUid user, EntityUid target)
    {
        var attempt = new IngestionAttemptEvent(IngestionSystem.DefaultFlags, user);
        RaiseLocalEvent(target, ref attempt);
        return !attempt.Cancelled;
    }

    private void OnCompleted(Entity<DamageableComponent> ent, ref CPRCompletedEvent args)
    {
        if (args.CanVentilate)
        {
            _damage.ChangeDamage(ent.Owner, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = -3 } },
                targetPart: TargetBodyPart.Vital, interruptsDoAfters: false, ignoreResistances: true);
        }

        if (_heart.GetHeart(ent) != null)
            EnsureComp<CPRPerfusionComponent>(ent);

        var threshold = _oxygen.GetThreshold(ent);
        var oxygenDamage = _damage.GetAllDamage(ent.Owner).DamageDict.GetValueOrDefault("Asphyxiation").Float();
        args.ReviveChance = threshold is { } limit ? Math.Clamp(1f - oxygenDamage / limit, 0f, 1f) : 0f;
        args.CanRevive = TryComp<PerishableComponent>(ent, out var perishable) &&
            !_rotting.IsRotten(ent) && _rotting.PerishStage((ent.Owner, perishable), SharedRottingSystem.MaxStages) <= 1 &&
            (!_heart.RequiresHeart(ent) || _heart.GetHeart(ent) != null);
    }

    private void OnRevived(Entity<MobStateComponent> ent, ref CPRRevivedEvent args)
    {
        if (_heart.GetHeart(ent) is { Comp: { } heart } installed && _heart.GetState(heart) == HeartState.Stopped)
            _heart.SetRate(installed.Owner, heart, heart.FibrillationCaps.X, true);
    }

    private void OnStopped(Entity<CPRPerfusionComponent> ent, ref CPRStoppedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            RemComp<CPRPerfusionComponent>(ent);
    }
}
