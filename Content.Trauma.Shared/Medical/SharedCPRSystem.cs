// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Trauma.Common.Body;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Arcane.Common.Medical.Respiration; // Arcane

namespace Content.Trauma.Shared.Medical;

public abstract partial class SharedCPRSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<ActiveCPRComponent> _activeQuery = default!;
    [Dependency] private EntityQuery<CPRTrainingComponent> _trainingQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private EntityQuery<InternalChildOrganComponent> _organQuery = default!;
    [Dependency] private EntityQuery<RottingComponent> _rottingQuery = default!;
    [Dependency] private EntityQuery<UnrevivableComponent> _unrevivableQuery = default!;

    /// <summary>
    /// Modifier for inhale volume on mobs that have CPR being done on them.
    /// </summary>
    public const float InhaleModifier = 3f;

    public static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRTrainingComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CPRTrainingComponent, ComponentShutdown>(OnPerformerShutdown); // Arcane

        SubscribeLocalEvent<ActiveCPRComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCPRComponent, CPRDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ActiveCPRComponent, ModifyInhaledVolumeEvent>(OnModifyInhaledVolume);
    }

    /// <summary>
    /// Returns true if a mob is having CPR done on it.
    /// </summary>
    public bool IsCPRActive(EntityUid uid)
        => _activeQuery.HasComp(uid);

    private void OnGetVerbs(Entity<CPRTrainingComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        // Arcane-Start: The server selects the procedure and validates its physiology.
        if (_net.IsClient)
            return;
        // Arcane-End
        var target = args.Target;
        if (!args.CanInteract || !args.CanAccess || !_mobQuery.TryComp(target, out var mob) || mob.CurrentState == MobState.Alive)
            return;

        // Arcane-Start: Resolve the displayed procedure on the authoritative side.
        var capabilities = new CPRCapabilitiesEvent(ent.Owner);
        RaiseLocalEvent(target, ref capabilities);
        if (capabilities.Handled && !capabilities.CanPerform)
            return;
        // Arcane-End
        args.Verbs.Add(new InnateVerb()
        {
            Act = () => StartCPR(ent, target),
            Text = capabilities.VerbText ?? Loc.GetString("cpr-verb"), // Arcane-Edit: Show the available breathing assistance.
            Icon = new SpriteSpecifier.Rsi(new("Interface/Alerts/human_alive.rsi"), "health4"),
            Priority = 2
        });
    }

    private void StartCPR(Entity<CPRTrainingComponent> ent, EntityUid target)
    {
        var identity = Identity.Entity(target, EntityManager);
        if (!CanStartCPR(ent, target, identity, out var capabilities)) // Arcane-Edit: Resolve the session's breathing capabilities.
            return;

        var userIdentity = Identity.Entity(ent, EntityManager);
        _popup.PopupEntity(Loc.GetString("cpr-start-second-person", ("target", identity)), target, ent);
        _popup.PopupEntity(Loc.GetString("cpr-start-second-person-patient", ("user", userIdentity)), target, target);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.Duration,
            new CPRDoAfterEvent(),
            eventTarget: target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
            BlockDuplicate = true
        };

        // Arcane-Edit-Start: Reserve the patient before starting and retain the procedure identity.
        var active = EnsureComp<ActiveCPRComponent>(target);
        active.Performer = ent.Owner;
        active.CanVentilate = capabilities.CanVentilate;
        if (!_doAfter.TryStartDoAfter(doAfterArgs, out var id))
        {
            RemComp<ActiveCPRComponent>(target);
            return;
        }
        active.DoAfterIndex = id.Value.Index;
        // Arcane-Edit-End
        // PlayPredicted is shitcode and doesnt spawn the same entity for client, can't do it nicely
        if (_net.IsClient || _audio.PlayPredicted(ent.Comp.Sound, ent, ent, ent.Comp.Sound.Params.WithLoop(true)) is not {} audio)
            return;

        active.Sound = audio.Entity;
        Dirty(target, active);
    }

    // Arcane-Edit-Start: Return capabilities without a second physiology query.
    private bool CanStartCPR(Entity<CPRTrainingComponent> ent, EntityUid target, EntityUid identity,
        out CPRCapabilitiesEvent capabilities)
    // Arcane-Edit-End
    {
        capabilities = default; // Arcane
        if (_activeQuery.HasComp(target))
        {
            _popup.PopupEntity(Loc.GetString("cpr-already-performing", ("entity", identity)), ent, ent, PopupType.Medium);
            return false;
        }

        return CanPerformCPR(ent, target, identity, out capabilities); // Arcane-Edit: Reuse the validated capabilities.
    }

    // Arcane-Edit-Start
    private bool CanPerformCPR(Entity<CPRTrainingComponent> ent, EntityUid target, EntityUid identity,
        out CPRCapabilitiesEvent capabilities)
    // Arcane-Edit-End
    {
        // Arcane-Start
        capabilities = new CPRCapabilitiesEvent(ent.Owner);
        RaiseLocalEvent(target, ref capabilities);
        // Arcane-End
        if (_rottingQuery.HasComp(target))
        {
            _popup.PopupEntity(Loc.GetString("cpr-target-rotting", ("entity", identity)), ent, ent, PopupType.LargeCaution);
            return false;
        }

        if (capabilities.Handled ? !capabilities.CanPerform : GetLungs(target) == null || GetLungs(ent) == null) // Arcane-Edit: Compression-only performers do not need lungs.
        {
            _popup.PopupEntity(Loc.GetString("cpr-target-cantbreathe", ("entity", identity)), ent, ent, PopupType.MediumCaution);
            return false;
        }

        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            _popup.PopupEntity(Loc.GetString("cpr-must-remove", ("clothing", outer)), ent, ent, PopupType.Medium);
            return false;
        }

        // popups done in ingestion system
        return capabilities.Handled || _ingestion.HasMouthAvailable(ent, ent) || !_ingestion.HasMouthAvailable(ent, target); // Arcane-Edit: The module validates both airways when providing breaths.
    }

    private void OnShutdown(Entity<ActiveCPRComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.Sound);
        // Arcane-Start
        var stopped = new CPRStoppedEvent();
        RaiseLocalEvent(ent.Owner, ref stopped);
        // Arcane-End
    }

    // Arcane-Start: Release sessions when the performer loses training or is deleted, and stop perfusion immediately.
    private void OnPerformerShutdown(Entity<CPRTrainingComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ActiveCPRComponent>();
        while (query.MoveNext(out var patient, out var active))
        {
            if (active.Performer == ent.Owner)
                StopCPR((patient, active));
        }
    }

    private void StopCPR(Entity<ActiveCPRComponent> ent)
    {
        var stopped = new CPRStoppedEvent();
        RaiseLocalEvent(ent.Owner, ref stopped);
        RemCompDeferred(ent, ent.Comp);
    }
    // Arcane-End

    private void OnDoAfter(Entity<ActiveCPRComponent> ent, ref CPRDoAfterEvent args)
    {
        var user = args.User;
        // Arcane-Start: A late event must not cancel a different operator or a new procedure.
        if (_net.IsClient || ent.Comp.Performer != user || ent.Comp.DoAfterIndex != args.DoAfter.Id.Index)
            return;
        // Arcane-End
        if (args.Cancelled || args.Handled)
        {
            StopCPR(ent); // Arcane-Edit: Release circulatory assistance with the session.
            return;
        }

        args.Handled = true;

        var identity = Identity.Entity(ent, EntityManager);
        if (!_trainingQuery.TryComp(user, out var training) ||
            !_mobQuery.TryComp(ent, out var mob) ||
            !CanPerformCPR((user, training), ent, identity, out var capabilities)) // Arcane-Edit: Refresh capabilities for this cycle.
        {
            StopCPR(ent); // Arcane-Edit: Release circulatory assistance with the session.
            return;
        }

        if (capabilities.CanVentilate && !HasHealthyLungs(ent)) // Arcane-Edit: Only artificial ventilation needs healthy lungs.
        {
            _popup.PopupEntity(Loc.GetString("cpr-failed-lungs-damaged", ("target", identity)), user, user, PopupType.LargeCaution);
            StopCPR(ent); // Arcane-Edit: Release circulatory assistance with the session.
            return;
        }

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        // Arcane-Start: Keep treatment before revival and assisted inhalation.
        ent.Comp.CanVentilate = capabilities.CanVentilate;
        var completed = new CPRCompletedEvent(user, capabilities.CanVentilate, training.ReviveChance);
        RaiseLocalEvent(ent.Owner, ref completed);
        // Arcane-End
        // Arcane-Edit-Start: Apply the patient's revival chance and notify successful circulation recovery.
        if (mob.CurrentState == MobState.Dead && completed.CanRevive && rand.Prob(completed.ReviveChance) && CanRevive(ent))
        {
            _mob.ChangeMobState(ent.Owner, MobState.Critical, origin: user);
            if (mob.CurrentState != MobState.Dead)
            {
                var revived = new CPRRevivedEvent(user);
                RaiseLocalEvent(ent.Owner, ref revived);
            }
        }
        // Arcane-Edit-End

        if (capabilities.CanVentilate && rand.Prob(training.InhaleChance) && HasHealthyLungs(ent)) // Arcane-Edit: Compressions do not provide breaths.
            TryInhale(ent); // technically should be transferring with the performer's lungs but whatever

        var isAlive = mob.CurrentState == MobState.Alive;
        args.Args.Delay = training.RepeatDuration; // Arcane
        args.Repeat = !isAlive;
        if (isAlive)
            StopCPR(ent); // Arcane-Edit: Release circulatory assistance with the session.
    }

    private void OnModifyInhaledVolume(Entity<ActiveCPRComponent> ent, ref ModifyInhaledVolumeEvent args)
    {
        // Arcane-Edit-Start: Chest compressions alone do not increase inhaled volume.
        if (ent.Comp.CanVentilate)
            args.Volume *= InhaleModifier;
        // Arcane-Edit-End
    }

    private bool HasHealthyLungs(EntityUid uid)
        // need healthy lungs for CPR to work, go tend organ damage first
        => GetLungs(uid) is {} lungs &&
            _organQuery.TryComp(lungs, out var organ) &&
            organ.OrganSeverity == OrganSeverity.Normal;

    private bool CanRevive(EntityUid uid)
        // no cpr major reviving ling husks
        => !_unrevivableQuery.HasComp(uid) &&
            // has to be below death threshold
            _threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
            _damageQuery.TryComp(uid, out var damage) &&
            _threshold.CheckVitalDamage((uid, damage)) < threshold;

    private EntityUid? GetLungs(EntityUid mob)
        => _body.GetOrgan(mob, LungsCategory);

    // respiration is serverside :(
    protected virtual void TryInhale(EntityUid uid)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent;
