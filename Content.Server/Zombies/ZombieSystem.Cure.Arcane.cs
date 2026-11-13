using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.HTN;
using Content.Server.StationEvents.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Speech.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    [Dependency] private ISerializationManager _zombieSerialization = default!;
    [Dependency] private SharedSolutionContainerSystem _zombieSolutions = default!;
    [Dependency] private HandOrganSystem _zombieHandOrgans = default!;
    [Dependency] private ExtraHandsEquipmentSystem _zombieEquipmentHands = default!;
    [Dependency] private SharedContainerSystem _zombieContainers = default!;
    [Dependency] private BodySystem _body = default!;

    private static readonly (string Reagent, int Dose, bool Inoculates)[] ZombieCures =
    [
        ("AmbuzolPlus", 5, true),
        ("Ambuzol", 10, false),
    ];
    private static readonly TimeSpan InfectionTick = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SymptomInterval = TimeSpan.FromSeconds(15);
    private const string ZombieHeldItems = "zombie-held-items";

    // Capture only components changed by the in-place transformation. The body, its organs, the mind,
    // and inventory remain the originals throughout infection and cure.
    private static readonly Type[] ChangedBodyComponents =
    [
        typeof(RespiratorComponent), typeof(BarotraumaComponent), typeof(SatiationComponent),
        typeof(ReproductiveComponent), typeof(ReproductivePartnerComponent), typeof(LegsParalyzedComponent),
        typeof(ComplexInteractionComponent), typeof(SentienceTargetComponent), typeof(ReplacementAccentComponent),
        typeof(PacifiedComponent), typeof(MeleeWeaponComponent), typeof(EmoteOnDamageComponent),
        typeof(AutoEmoteComponent), typeof(PryingComponent), typeof(TemperatureDamageComponent),
        typeof(HTNComponent), typeof(GhostRoleComponent), typeof(GhostTakeoverAvailableComponent),
    ];

    private static readonly Type[] ChangedSiliconComponents =
    [
        typeof(ActiveRadioComponent), typeof(IntrinsicRadioReceiverComponent), typeof(IntrinsicRadioTransmitterComponent),
    ];

    private readonly HashSet<EntityUid> _zombieCureCandidates = new();
    private readonly List<EntityUid> _zombieCureBatch = new();

    private void InitializeCure()
    {
        SubscribeLocalEvent<ZombieComponent, SolutionChangedEvent>(OnZombieSolutionChanged);
        SubscribeLocalEvent<StomachComponent, SolutionChangedEvent>(OnZombieStomachSolutionChanged);
        SubscribeLocalEvent<ZombieRecoveryComponent, BeingGibbedEvent>(OnZombieGibbed);
    }

    private void CaptureZombieState(EntityUid target)
    {
        if (HasComp<IncurableZombieComponent>(target))
            return;

        var snapshot = AddComp<ZombieRecoveryComponent>(target);
        foreach (var type in ChangedBodyComponents)
            CaptureComponent(target, type, snapshot.BodyComponents);

        if (HasComp<SiliconZombieComponent>(target))
        {
            foreach (var type in ChangedSiliconComponents)
                CaptureComponent(target, type, snapshot.BodyComponents);
        }

        if (TryComp<HandsComponent>(target, out var hands))
        {
            snapshot.Hands = _zombieSerialization.CreateCopy(hands, notNullableOverride: true);
            var attachedHandIds = _zombieHandOrgans.GetBodyHandIds(target);
            attachedHandIds.UnionWith(_zombieEquipmentHands.GetEquipmentHandIds(target));
            foreach (var name in _hands.EnumerateHands(target))
            {
                if (!attachedHandIds.Contains(name) && _hands.TryGetHand(target, name, out var hand))
                    snapshot.HandDefinitions.Add((name, hand.Value));
            }
            snapshot.ActiveHand = _hands.GetActiveHand(target);
        }

        snapshot.HadCombatMode = HasComp<CombatModeComponent>(target);
        snapshot.CanDisarm = _combat.GetCanDisarm(target);
        if (TryComp<PullerComponent>(target, out var puller))
        {
            snapshot.Puller = _zombieSerialization.CreateCopy(puller, notNullableOverride: true);
            snapshot.Puller.Pulling = null;
        }
        snapshot.Factions = TryComp<NpcFactionMemberComponent>(target, out var factions) ? [.. factions.Factions] : null;
        snapshot.Modifier = CompOrNull<DamageableComponent>(target)?.DamageModifierSetId;
        snapshot.HadCannotSuicide = _tag.HasTag(target, CannotSuicideTag);
        snapshot.HadInvalidForSpawnSpell = _tag.HasTag(target, InvalidForGlobalSpawnSpellTag);
        if (TryComp<BloodstreamComponent>(target, out var blood))
        {
            snapshot.BloodLossThreshold = blood.BloodlossThreshold;
            snapshot.BloodIncreaseEnabled = blood.BloodIncreaseEnabled;
        }
    }

    private void CaptureComponent(EntityUid target, Type type, Dictionary<Type, IComponent?> snapshot)
    {
        snapshot[type] = TryComp(target, type, out var component)
            ? _zombieSerialization.CreateCopy(component, notNullableOverride: true)
            : null;
    }

    private void PreserveZombieHeldItems(EntityUid uid)
    {
        if (!TryComp<ZombieRecoveryComponent>(uid, out var snapshot))
            return;

        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!_hands.TryGetHeldItem(uid, hand, out var held, hideVirtualItems: true))
                continue;

            // A bound item can refuse normal removal from the hand. Do not let RemoveHands delete it with the slot.
            if (_zombieContainers.TryGetContainingContainer(uid, held.Value, out var handSlot))
                _zombieContainers.Remove(held.Value, handSlot, force: true);

            var container = snapshot.HeldContainer ??= _zombieContainers.EnsureContainer<Container>(uid, ZombieHeldItems);
            if (_zombieContainers.Insert(held.Value, container))
                snapshot.HeldItems[held.Value] = hand;
        }
    }

    private void RestoreZombieHeldItems(EntityUid uid, ZombieRecoveryComponent snapshot)
    {
        if (snapshot.HeldContainer is not { } container)
            return;

        foreach (var (item, hand) in snapshot.HeldItems)
        {
            // The item may still be marked unremovable until the next tick after zombification.
            if (!container.Contains(item) || !_zombieContainers.Remove(item, container, force: true))
                continue;

            if (!_hands.TryPickup(uid, item, hand, animate: false, checkActionBlocker: false))
                _hands.TryPickupAnyHand(uid, item, animate: false, checkActionBlocker: false);
        }

        _zombieContainers.EmptyContainer(container, force: true);
        _zombieContainers.ShutdownContainer(container);
    }

    private void OnZombieGibbed(Entity<ZombieRecoveryComponent> ent, ref BeingGibbedEvent args)
    {
        if (ent.Comp.HeldContainer is not { } container)
            return;

        args.Giblets.UnionWith(_zombieContainers.EmptyContainer(container, force: true));
        _zombieContainers.ShutdownContainer(container);
        ent.Comp.HeldContainer = null;
    }

    private void OnZombieSolutionChanged(Entity<ZombieComponent> ent, ref SolutionChangedEvent args)
    {
        if (!HasComp<SiliconZombieComponent>(ent) &&
            TryComp<BloodstreamComponent>(ent, out var blood) &&
            blood.BloodSolution?.Owner == args.Solution.Owner)
            _zombieCureCandidates.Add(ent.Owner);
    }

    private void OnZombieStomachSolutionChanged(Entity<StomachComponent> ent, ref SolutionChangedEvent args)
    {
        if (ent.Comp.Solution?.Owner == args.Solution.Owner &&
            _body.GetBody(ent) is { } body && HasComp<ZombieComponent>(body) && !HasComp<SiliconZombieComponent>(body))
            _zombieCureCandidates.Add(body);
    }

    private void QueueZombieCureOnIncapacitation(EntityUid uid, MobState state)
    {
        if (state is MobState.SoftCrit or MobState.Critical or MobState.Dead && !HasComp<SiliconZombieComponent>(uid))
            _zombieCureCandidates.Add(uid);
    }

    private void UpdateInfectionSymptoms(EntityUid uid, PendingZombieComponent pending)
    {
        pending.SymptomCooldown -= InfectionTick;
        if (pending.SymptomCooldown > TimeSpan.Zero)
            return;

        pending.SymptomCooldown = SymptomInterval;
        var symptom = new PendingZombieSymptomEvent();
        RaiseLocalEvent(uid, ref symptom);
        if (!symptom.Handled)
            _chat.TryEmoteWithoutChat(uid, "Cough", ignoreActionBlocker: true);
    }

    private void UpdateZombieCures()
    {
        // Solutions raise changes synchronously. Defer the cure until the current injection or
        // transfer finishes, and also retry when a treated zombie later becomes incapacitated.
        _zombieCureBatch.Clear();
        _zombieCureBatch.AddRange(_zombieCureCandidates);
        foreach (var uid in _zombieCureBatch)
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
            {
                _zombieCureCandidates.Remove(uid);
                continue;
            }

            if (MetaData(uid).EntityPaused)
                continue;

            _zombieCureCandidates.Remove(uid);
            if (!TryComp<ZombieComponent>(uid, out var zombie) ||
                !TryComp<ZombieRecoveryComponent>(uid, out var snapshot) ||
                HasComp<IncurableZombieComponent>(uid) ||
                HasComp<SiliconZombieComponent>(uid) ||
                !_mobState.IsCritical(uid) && !_mobState.IsDead(uid))
                continue;

            if (TryComp<BloodstreamComponent>(uid, out var blood) &&
                _zombieSolutions.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution) &&
                TryCureFromSolution(uid, zombie, snapshot, blood.BloodSolution!.Value))
                continue;

            foreach (var stomach in _body.EnumerateOrgans<StomachComponent>(uid))
            {
                if (_zombieSolutions.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp2.Solution) &&
                    TryCureFromSolution(uid, zombie, snapshot, stomach.Comp2.Solution!.Value))
                    break;
            }
        }
    }

    private bool TryCureFromSolution(EntityUid uid, ZombieComponent zombie, ZombieRecoveryComponent snapshot,
        Entity<SolutionComponent> solution)
    {
        foreach (var (reagent, dose, inoculates) in ZombieCures)
        {
            if (solution.Comp.Solution.GetTotalPrototypeQuantity(reagent) < dose)
                continue;

            _zombieSolutions.RemoveReagent(solution, reagent, dose);
            RestoreZombie(uid, zombie, snapshot, inoculates);
            return true;
        }

        return false;
    }

    public bool TryPurgeSiliconZombie(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) ||
            !HasComp<SiliconZombieComponent>(uid) || HasComp<IncurableZombieComponent>(uid))
            return false;

        if (TryComp<ZombieComponent>(uid, out var zombie))
        {
            if (!TryComp<ZombieRecoveryComponent>(uid, out var snapshot) ||
                !_mobState.IsCritical(uid) && !_mobState.IsDead(uid))
                return false;

            RestoreZombie(uid, zombie, snapshot, inoculates: false);
            return true;
        }

        if (!HasComp<PendingZombieComponent>(uid) && !HasComp<ZombifyOnDeathComponent>(uid))
            return false;

        RemComp<PendingZombieComponent>(uid);
        RemComp<ZombifyOnDeathComponent>(uid);
        return true;
    }

    private void RestoreZombie(EntityUid uid, ZombieComponent zombie, ZombieRecoveryComponent snapshot, bool inoculates)
    {
        _zombieCureCandidates.Remove(uid);
        RemComp<ZombifyOnDeathComponent>(uid);
        RemComp<PendingZombieComponent>(uid);
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _role.MindRemoveRole<ZombieRoleComponent>((mindId, mind));

        UnZombify(uid, uid, zombie);
        RemComp(uid, zombie);

        foreach (var (type, previous) in snapshot.BodyComponents)
        {
            RemComp(uid, type);
            if (previous != null)
                AddComp(uid, previous);
        }

        if (snapshot.Hands is { } hands)
            _hands.RestoreAfterZombification(uid, hands, snapshot.HandDefinitions, snapshot.ActiveHand);
        RestoreZombieHeldItems(uid, snapshot);

        if (snapshot.HadCombatMode)
            _combat.SetCanDisarm(uid, snapshot.CanDisarm);
        else
            RemComp<CombatModeComponent>(uid);

        if (snapshot.Puller is { } puller && !HasComp<PullerComponent>(uid))
            AddComp(uid, puller);
        _damageable.SetDamageModifierSetId(uid, snapshot.Modifier);

        _faction.ClearFactions(uid);
        if (snapshot.Factions is { } factions)
            _faction.AddFactions(uid, factions);
        else
            RemComp<NpcFactionMemberComponent>(uid);

        if (snapshot.BloodLossThreshold is { } threshold)
        {
            _bloodstream.SetBloodLossThreshold(uid, threshold);
            _bloodstream.ChangeBloodIncreaseEnabled(uid, snapshot.BloodIncreaseEnabled);
        }

        if (snapshot.HadCannotSuicide)
            _tag.AddTag(uid, CannotSuicideTag);
        if (snapshot.HadInvalidForSpawnSpell)
            _tag.AddTag(uid, InvalidForGlobalSpawnSpellTag);
        if (inoculates)
            EnsureComp<ZombieImmuneComponent>(uid);

        RemComp(uid, snapshot);
        _nameMod.RefreshNameModifiers(uid);
        _identity.QueueIdentityUpdate(uid);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _popup.PopupEntity(Loc.GetString("zombie-cure-success"), uid, PopupType.Medium);
    }
}

[RegisterComponent]
public sealed partial class ZombieRecoveryComponent : Component
{
    public readonly Dictionary<Type, IComponent?> BodyComponents = new();
    public HandsComponent? Hands;
    public Container? HeldContainer;
    public readonly Dictionary<EntityUid, string> HeldItems = new();
    public readonly List<(string Name, Hand Definition)> HandDefinitions = new();
    public string? ActiveHand;
    public bool HadCombatMode;
    public bool? CanDisarm;
    public PullerComponent? Puller;
    public HashSet<ProtoId<NpcFactionPrototype>>? Factions;
    public ProtoId<DamageModifierSetPrototype>? Modifier;
    public bool HadCannotSuicide;
    public bool HadInvalidForSpawnSpell;
    public float? BloodLossThreshold;
    public bool BloodIncreaseEnabled;
}
