using Content.Arcane.Shared.CloneProjector;
using Content.Goobstation.Shared.Holograms;
using Content.Medical.Shared.Wounds;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.CloneProjector;

public sealed partial class CloneProjectorSystem : EntitySystem
{
    private const string BeltSlot = "belt";
    private const string StorageId = "gemini-projection";
    private static readonly EntProtoId GeminiRole = "MindRoleGemini";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CloneProjectorComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CloneProjectorComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CloneProjectorComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<CloneProjectorComponent, GetItemActionsEvent>(OnItemActions);
        SubscribeLocalEvent<CloneProjectorComponent, CloneProjectorActivatedEvent>(OnActivated);
        SubscribeLocalEvent<CloneProjectorComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbs);
        SubscribeLocalEvent<CloneProjectorComponent, ExaminedEvent>(OnProjectorExamined);
        SubscribeLocalEvent<CloneProjectorComponent, EmpPulseEvent>(OnProjectorEmp);
        SubscribeLocalEvent<CloneProjectorComponent, EntityTerminatingEvent>(OnProjectorTerminating);
        SubscribeLocalEvent<GeminiWearerComponent, MobStateChangedEvent>(OnWearerStateChanged);
        SubscribeLocalEvent<GeminiWearerComponent, PlayerDetachedEvent>(OnWearerDetached);
        SubscribeLocalEvent<GeminiWearerComponent, EntityTerminatingEvent>(OnWearerTerminating);
        SubscribeLocalEvent<GeminiProjectionComponent, MobStateChangedEvent>(OnProjectionStateChanged);
        SubscribeLocalEvent<GeminiProjectionComponent, EntityTerminatingEvent>(OnProjectionTerminating);
        SubscribeLocalEvent<GeminiProjectionComponent, BeforeDamageChangedEvent>(OnProjectionDamage);
        SubscribeLocalEvent<GeminiProjectionComponent, MindRemovedMessage>(OnProjectionMindRemoved);
        SubscribeLocalEvent<GeminiProjectionComponent, ExaminedEvent>(OnProjectionExamined);
        SubscribeLocalEvent<GeminiProjectionComponent, EmpPulseEvent>(OnProjectionEmp);
        SubscribeLocalEvent<GeminiOrganComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<GeminiOrganComponent, BeforeDamageChangedEvent>(OnOrganDamage);
        SubscribeLocalEvent<GeminiEquipmentComponent, BeingUnequippedAttemptEvent>(OnCopyUnequipAttempt);
        SubscribeLocalEvent<GeminiEquipmentComponent, EntityTerminatingEvent>(OnCopyTerminating);
    }

    private bool CanWear(Entity<CloneProjectorComponent> projector, EntityUid host) =>
        HasComp<HumanoidProfileComponent>(host)
        && !HasComp<GeminiProjectionComponent>(host)
        && !_whitelist.IsWhitelistPass(projector.Comp.UserBlacklist, host);

    private bool CanUse(Entity<CloneProjectorComponent> projector, EntityUid host) =>
        projector.Comp.CurrentHost == host
        && _timing.CurTime >= projector.Comp.ReadyAt
        && _mobState.IsAlive(host)
        && CanWear(projector, host);

    private void OnEquipAttempt(Entity<CloneProjectorComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Slot == BeltSlot && !CanWear(ent, args.EquipTarget))
            args.Cancel();
    }

    private void OnEquipped(Entity<CloneProjectorComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != BeltSlot || !CanWear(ent, args.EquipTarget))
            return;

        var host = args.EquipTarget;
        ent.Comp.CurrentHost = host;
        EnsureComp<GeminiWearerComponent>(host).Projector = ent;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("gemini-projector-installed"), host, host);
        StunWearer(ent, host, ent.Comp.StunDuration);
    }

    private void OnUnequipped(Entity<CloneProjectorComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Slot != BeltSlot || ent.Comp.CurrentHost != args.EquipTarget)
            return;

        ReleaseWearer(ent);
        var host = args.EquipTarget;
        if (TerminatingOrDeleted(host))
            return;

        _popup.PopupEntity(Loc.GetString("gemini-projector-removed"), host, host);
        StunWearer(ent, host, ent.Comp.StunDuration);
    }

    private void OnItemActions(Entity<CloneProjectorComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } slots || (slots & SlotFlags.BELT) == 0)
            return;

        if (args.IsEquipping && CanWear(ent, args.User))
        {
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
            if (ent.Comp.ActionEntity is { } action && ent.Comp.ReadyAt > _timing.CurTime)
                _actions.SetCooldown(action, ent.Comp.ReadyAt - _timing.CurTime);
            Dirty(ent);
        }
        else
        {
            args.AddAction(ent.Comp.ActionEntity);
        }
    }

    private void OnActivated(Entity<CloneProjectorComponent> ent, ref CloneProjectorActivatedEvent args)
    {
        if (args.Handled || !CanUse(ent, args.Performer))
            return;

        if (TryComp(ent.Comp.CloneUid, out GeminiProjectionComponent? existing) && existing.Deployed)
        {
            args.Handled = Recall(ent, (ent.Comp.CloneUid!.Value, existing));
            return;
        }

        if ((!TryComp(ent.Comp.CloneUid, out existing) || !_mobState.IsAlive(ent.Comp.CloneUid.Value))
            && !Regenerate(ent, args.Performer, preserveMind: true))
            return;

        if (ent.Comp.CloneUid is not { } clone || !TryComp(clone, out GeminiProjectionComponent? projection))
            return;

        if (!_containers.TryRemoveFromContainer(clone))
            return;

        projection.Deployed = true;
        if (!_mind.TryGetMind(clone, out _, out _))
            OfferRole(ent, clone);

        args.Handled = true;
        _popup.PopupEntity(
            Loc.GetString("gemini-projector-clone-created", ("user", Identity.Name(args.Performer, EntityManager))),
            args.Performer, PopupType.Medium);
    }

    private void OnVerbs(Entity<CloneProjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanInteract || !args.CanAccess || !args.CanComplexInteract || !CanUse(ent, user))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => RegenerateWithCooldown(ent, user, preserveMind: true),
            Text = Loc.GetString("gemini-projector-regenerate-verb"),
            Message = Loc.GetString("gemini-projector-regenerate-verb-text"),
            Icon = new SpriteSpecifier.Rsi(new("Mobs/Silicon/station_ai.rsi"), "default"),
        });
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => RegenerateWithCooldown(ent, user, preserveMind: false),
            Text = Loc.GetString("gemini-projector-reboot-verb"),
            Message = Loc.GetString("gemini-projector-reboot-verb-text"),
            Icon = new SpriteSpecifier.Rsi(new("_Goobstation/Actions/modsuit.rsi"), "activate"),
        });
    }

    private void RegenerateWithCooldown(Entity<CloneProjectorComponent> ent, EntityUid user, bool preserveMind)
    {
        if (CanUse(ent, user) && Regenerate(ent, user, preserveMind))
            SetCooldown(ent, ent.Comp.DestroyedCooldown);
    }

    private bool Regenerate(Entity<CloneProjectorComponent> ent, EntityUid host, bool preserveMind)
    {
        if (!TryComp(host, out HumanoidProfileComponent? profile)
            || !_prototypes.TryIndex(profile.Species, out var species))
            return false;

        var storage = _containers.EnsureContainer<Container>(ent, StorageId);
        var clone = Spawn(species.Prototype, Transform(host).Coordinates);
        if (!_containers.Insert(clone, storage))
        {
            QueueDel(clone);
            return false;
        }

        _visualBody.CopyAppearanceFrom(host, clone);
        if (ent.Comp.RemovedComponents is { } removed)
            EntityManager.RemoveComponents(clone, removed);
        if (ent.Comp.AddedComponents is { } added)
            EntityManager.AddComponents(clone, added, removeExisting: true);

        var projection = EnsureComp<GeminiProjectionComponent>(clone);
        projection.Projector = ent;
        var hologram = EnsureComp<GeminiHologramComponent>(clone);
        hologram.RestrictRangedWeapons = ent.Comp.RestrictRangedWeapons;
        Dirty(clone, hologram);
        EnsureComp<HologramVisualsComponent>(clone);
        foreach (var organ in _body.GetOrgans(clone))
        {
            EnsureComp<GeminiOrganComponent>(organ);
            if (!TryComp(organ.Owner, out WoundableComponent? woundable))
                continue;

            woundable.CanRemove = false;
            woundable.CanBleed = false;
            woundable.AllowWounds = false;
            Dirty(organ.Owner, woundable);
        }

        _damage.SetDamageModifierSetId(clone, ent.Comp.CloneDamageModifierSet);
        var name = Identity.Name(host, EntityManager);
        _metadata.SetEntityName(clone, $"{name} {Loc.GetString(ent.Comp.NameSuffix)}");
        CopyEquipment(ent, host, (clone, projection));

        var oldClone = ent.Comp.CloneUid;
        ent.Comp.CloneUid = clone;
        Dirty(ent);
        if (oldClone is { } old && TryComp(old, out GeminiProjectionComponent? oldProjection))
        {
            if (preserveMind && _mind.TryGetMind(old, out var mindId, out var mind))
                _mind.TransferTo(mindId, clone, ghostCheckOverride: true, mind: mind);
            Retire((old, oldProjection));
        }

        return true;
    }

    private void OfferRole(Entity<CloneProjectorComponent> projector, EntityUid clone)
    {
        if (HasComp<GhostRoleComponent>(clone))
            return;

        var role = new GhostRoleComponent
        {
            RoleName = projector.Comp.GhostRoleName,
            RoleDescription = projector.Comp.GhostRoleDescription,
            RoleRules = projector.Comp.GhostRoleRules,
            MindRoles = [GeminiRole],
            MakeSentient = false,
        };
        AddComp(clone, role);
    }

    private void ReleaseWearer(Entity<CloneProjectorComponent> ent)
    {
        if (ent.Comp.CurrentHost is { } host
            && TryComp(host, out GeminiWearerComponent? wearer)
            && wearer.Projector == ent.Owner)
            RemComp(host, wearer);

        ent.Comp.CurrentHost = null;
        if (ent.Comp.CloneUid is { } clone && TryComp(clone, out GeminiProjectionComponent? projection))
            Retire((clone, projection));
        ent.Comp.CloneUid = null;
        if (!TerminatingOrDeleted(ent))
            Dirty(ent);
    }

    private void OnWearerStateChanged(Entity<GeminiWearerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RecallFromWearer(ent);
    }

    private void OnWearerDetached(Entity<GeminiWearerComponent> ent, ref PlayerDetachedEvent args) =>
        RecallFromWearer(ent);

    private void RecallFromWearer(Entity<GeminiWearerComponent> ent)
    {
        if (TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector)
            && !TerminatingOrDeleted(ent.Comp.Projector)
            && projector.CloneUid is { } clone
            && TryComp(clone, out GeminiProjectionComponent? projection))
            Recall((ent.Comp.Projector, projector), (clone, projection));
    }

    private void OnWearerTerminating(Entity<GeminiWearerComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector))
            ReleaseWearer((ent.Comp.Projector, projector));
    }

    private void OnProjectorTerminating(Entity<CloneProjectorComponent> ent, ref EntityTerminatingEvent args) =>
        ReleaseWearer(ent);

    private void OnProjectionTerminating(Entity<GeminiProjectionComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.Retiring)
        {
            ent.Comp.Retiring = true;
            ReleaseEquipment(ent, destroyCopies: true);
        }

        if (TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector)
            && projector.CloneUid == ent.Owner)
        {
            projector.CloneUid = null;
            if (!TerminatingOrDeleted(ent.Comp.Projector))
                Dirty(ent.Comp.Projector, projector);
        }
    }

    private void Retire(Entity<GeminiProjectionComponent> ent)
    {
        if (ent.Comp.Retiring)
            return;

        ent.Comp.Retiring = true;
        RemComp<GhostRoleComponent>(ent);
        ReleaseEquipment(ent, destroyCopies: true);
        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, null, ghostCheckOverride: true, mind: mind);
        QueueDel(ent);
    }

    private bool Recall(Entity<CloneProjectorComponent> projector, Entity<GeminiProjectionComponent> clone)
    {
        if (!clone.Comp.Deployed || clone.Comp.Retiring)
            return false;

        clone.Comp.Deployed = false;
        RemComp<GhostRoleComponent>(clone);
        ReleaseEquipment(clone, destroyCopies: false);
        _popup.PopupCoordinates(Loc.GetString("gemini-projector-clone-retrieved", ("target", Name(clone))),
            Transform(clone).Coordinates, PopupType.Medium);

        var storage = _containers.EnsureContainer<Container>(projector, StorageId);
        if (_containers.Insert(clone.Owner, storage))
            return true;

        projector.Comp.CloneUid = null;
        Dirty(projector);
        Retire(clone);
        return true;
    }

    private void OnProjectionStateChanged(Entity<GeminiProjectionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !ent.Comp.Deployed
            || !TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector))
            return;

        var source = (ent.Comp.Projector, projector);
        Recall(source, ent);
        SetCooldown(source, projector.DestroyedCooldown);
        if (projector.CurrentHost is not { } host || TerminatingOrDeleted(host))
            return;

        _popup.PopupEntity(Loc.GetString("gemini-projector-clone-destroyed"), host, host, PopupType.LargeCaution);
        if (!projector.DoStun)
            return;

        StunWearer(source, host, projector.StunDuration);
        _damage.TryChangeDamage(host, projector.DamageOnDestroyed, ignoreResistances: true, canMiss: false);
    }

    private void OnProjectorEmp(Entity<CloneProjectorComponent> ent, ref EmpPulseEvent args) =>
        ApplyEmp(ent, ref args);

    private void OnProjectionEmp(Entity<GeminiProjectionComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Deployed && TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector))
            ApplyEmp((ent.Comp.Projector, projector), ref args);
    }

    private void ApplyEmp(Entity<CloneProjectorComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
        SetCooldown(ent, args.Duration);
        if (ent.Comp.CloneUid is not { } clone || !TryComp(clone, out GeminiProjectionComponent? projection)
            || !Recall(ent, (clone, projection)) || ent.Comp.CurrentHost is not { } host)
            return;

        var stun = args.Duration < ent.Comp.StunDuration ? args.Duration : ent.Comp.StunDuration;
        StunWearer(ent, host, stun);
    }

    private void SetCooldown(Entity<CloneProjectorComponent> ent, TimeSpan duration)
    {
        var end = _timing.CurTime + duration;
        if (end <= ent.Comp.ReadyAt)
            return;

        ent.Comp.ReadyAt = end;
        if (ent.Comp.ActionEntity is { } action)
            _actions.SetCooldown(action, duration);
    }

    private void StunWearer(Entity<CloneProjectorComponent> ent, EntityUid host, TimeSpan duration)
    {
        if (ent.Comp.DoStun)
            _stun.TryUpdateParalyzeDuration(host, duration);
    }

    private void OnProjectionDamage(Entity<GeminiProjectionComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.Deployed)
            args.Cancelled = true;
    }

    private void OnOrganDamage(Entity<GeminiOrganComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (TryComp(_body.GetBody(ent), out GeminiProjectionComponent? projection) && !projection.Deployed)
            args.Cancelled = true;
    }

    private void OnProjectionMindRemoved(Entity<GeminiProjectionComponent> ent, ref MindRemovedMessage args)
    {
        if (!HasComp<GeminiProjectionComponent>(args.TransferEntity))
            _roles.MindRemoveRole<GeminiRoleComponent>(args.Mind.AsNullable());
    }

    private void OnProjectionExamined(Entity<GeminiProjectionComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && TryComp(ent.Comp.Projector, out CloneProjectorComponent? projector))
            args.PushMarkup(Loc.GetString(projector.FlavorText));
    }

    private void OnProjectorExamined(Entity<CloneProjectorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var active = TryComp(ent.Comp.CloneUid, out GeminiProjectionComponent? projection) && projection.Deployed;
        args.PushMarkup(Loc.GetString("clone-projector-examined-status", ("cloneStatus", active)));
        if (ent.Comp.CloneUid is { } clone && TryComp(clone, out DamageableComponent? damage)
            && _thresholds.TryGetDeadThreshold(clone, out var threshold) && threshold > 0)
        {
            var remaining = Math.Clamp((float) ((threshold.Value - _damage.GetTotalDamage((clone, damage))) / threshold.Value) * 100f, 0f, 100f);
            args.PushMarkup(Loc.GetString("clone-projector-examined-health", ("cloneHealth", remaining)));
        }
    }

    private void OnOrganRemoved(Entity<GeminiOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            QueueDel(ent);
    }
}
