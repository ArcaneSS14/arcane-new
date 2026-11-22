using System.Linq;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Administration;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Intellicard;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.PAI;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.Wizard.TimeStop;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string PaiSlotId = "mod-pai";
    private static readonly EntProtoId AssistantInterfaceAction = "ActionModAssistantInterface";
    private static readonly (MoveButtons Button, Direction Direction)[] AssistantDirections =
    [
        (MoveButtons.Up, Direction.North), (MoveButtons.Down, Direction.South),
        (MoveButtons.Left, Direction.West), (MoveButtons.Right, Direction.East),
    ];

    [Dependency] private SharedMoverController _assistantMover = default!;
    [Dependency] private SharedEyeSystem _assistantEye = default!;
    [Dependency] private MobStateSystem _assistantMobState = default!;
    [Dependency] private SharedPhysicsSystem _assistantPhysics = default!;

    private void InitializeAssistants()
    {
        SubscribeLocalEvent<ModSuitComponent, ContainerIsInsertingAttemptEvent>(OnAssistantInserting);
        SubscribeLocalEvent<ModSuitComponent, ContainerIsRemovingAttemptEvent>(OnAssistantRemoving);
        SubscribeLocalEvent<ModSuitComponent, ModAssistantInterfaceEvent>(OnAssistantInterface);
        SubscribeLocalEvent<ModSuitComponent, BoundUIOpenedEvent>(OnAssistantUiOpened);
        SubscribeLocalEvent<ModAssistantComponent, DirectionInputEvent>(OnAssistantInput);
        SubscribeLocalEvent<ModAssistantComponent, WalkingInputEvent>(OnAssistantWalkingInput);
        SubscribeLocalEvent<ModAssistantComponent, ActionTargetOriginEvent>(OnAssistantTargetOrigin);
        SubscribeLocalEvent<ModAssistantComponent, PlayerDetachedEvent>(OnAssistantDetached);
        SubscribeLocalEvent<ModAssistantComponent, EffectiveMoverChangedEvent>(OnAssistantRelayChanged);
        SubscribeLocalEvent<ModAssistantMoverComponent, DirectionInputEvent>(OnWearerInput);
        SubscribeLocalEvent<ModAssistantMoverComponent, WalkingInputEvent>(OnWearerWalkingInput);
        SubscribeLocalEvent<ModAssistantMoverComponent, MoveInputEvent>(OnWearerMovementChanged);
        SubscribeLocalEvent<ModAssistantMoverComponent, PlayerDetachedEvent>(OnWearerDetached);
        SubscribeLocalEvent<ModAssistantMoverComponent, PlayerAttachedEvent>(OnWearerAttached);
        SubscribeLocalEvent<ModAssistantComponent, InteractionAttemptEvent>(OnAssistantInteraction, after: [typeof(SharedInteractionSystem)]);
        SubscribeLocalEvent<ModAssistantComponent, MindRemovedMessage>(OnAssistantMindRemoved);
        SubscribeLocalEvent<ModAssistantComponent, MindAddedMessage>(OnAssistantMindAdded);
        SubscribeLocalEvent<ModAssistantComponent, EntityTerminatingEvent>(OnAssistantTerminating);
        SubscribeLocalEvent<ModAssistantMoverComponent, UpdateCanMoveEvent>(OnAssistantCanMove,
            after: [typeof(MobStateSystem), typeof(SharedStunSystem), typeof(SharedInteractionSystem), typeof(SharedMoverController)]);
        SubscribeLocalEvent<ModAssistantMoverComponent, RefreshMovementSpeedModifiersEvent>(OnAssistantSpeed);
        SubscribeLocalEvent<ModModuleComponent, ModModuleInstalledEvent>(OnAssistantModuleInstalled);
        SubscribeLocalEvent<ModModuleComponent, ModModuleRemovedEvent>(OnAssistantModuleRemoved);
    }

    private static bool IsAssistantSlot(string id) => id is PaiSlotId or StationAiHolderComponent.Container;

    private void OnAssistantInserting(Entity<ModSuitComponent> suit, ref ContainerIsInsertingAttemptEvent args)
    {
        if (_net.IsClient || _timing.ApplyingState || !IsAssistantSlot(args.Container.ID))
            return;

        if (suit.Comp.Projected || !_wires.IsPanelOpen(suit.Owner)
            || GetStoredAssistant(suit) is { } occupied && occupied != args.EntityUid
            || !TryComp(args.EntityUid, out MindContainerComponent? mind) || !mind.HasMind
            || args.Container.ID == PaiSlotId && !HasComp<PAIComponent>(args.EntityUid)
            || args.Container.ID == StationAiHolderComponent.Container && !HasComp<StationAiCustomizationComponent>(args.EntityUid))
            args.Cancel();
    }

    private void OnAssistantRemoving(Entity<ModSuitComponent> suit, ref ContainerIsRemovingAttemptEvent args)
    {
        if (!_net.IsClient && !_timing.ApplyingState && IsAssistantSlot(args.Container.ID) && !_wires.IsPanelOpen(suit.Owner))
            args.Cancel();
    }

    private EntityUid? GetStoredAssistant(Entity<ModSuitComponent> suit) =>
        _itemSlots.GetItemOrNull(suit.Owner, StationAiHolderComponent.Container)
        ?? _itemSlots.GetItemOrNull(suit.Owner, PaiSlotId);

    private bool TryUseAssistantCard(Entity<ModSuitComponent> suit, EntityUid user, EntityUid used)
    {
        if (!HasComp<IntellicardComponent>(used) || !HasComp<StationAiHolderComponent>(suit)
            || !_wires.IsPanelOpen(suit.Owner) || !_interaction.InRangeAndAccessible(user, suit.Owner))
            return false;

        var interaction = new AfterInteractEvent(user, used, suit, Transform(suit).Coordinates, true);
        RaiseLocalEvent(used, interaction);
        return interaction.Handled;
    }

    private void RefreshAssistant(Entity<ModSuitComponent> suit)
    {
        var installed = GetStoredAssistant(suit);
        if (suit.Comp.Assistant == installed)
            return;

        if (suit.Comp.Assistant is { } old && TryComp(old, out ModAssistantComponent? previous))
        {
            StopAssistantMovement((old, previous));
            ClearAssistantActions((old, previous));
            if (previous.PreviousDrawFov is { } drawFov && !TerminatingOrDeleted(old))
            {
                _assistantEye.SetDrawFov(old, drawFov);
                _assistantEye.SetTarget(old, null);
            }
            if (previous.AddedInput)
                RemComp<InputMoverComponent>(old);
            RemComp(old, previous);
        }

        suit.Comp.Assistant = installed;
        Dirty(suit);
        if (installed is not { } assistant || TerminatingOrDeleted(assistant))
            return;

        var component = EnsureComp<ModAssistantComponent>(assistant);
        component.Suit = suit;
        component.AddedInput = !HasComp<InputMoverComponent>(assistant);
        EnsureComp<InputMoverComponent>(assistant);
        if (suit.Comp.ModulesContainer is { } modules)
        {
            foreach (var module in modules.ContainedEntities)
            {
                if (TryComp(module, out ModModuleComponent? installedModule) && installedModule.Pinned)
                    component.PinnedModules.Add(module);
            }
        }
        if (TryComp(assistant, out EyeComponent? eye))
        {
            component.PreviousDrawFov = eye.DrawFov;
            _assistantEye.SetDrawFov(assistant, true);
            _assistantEye.SetTarget(assistant, suit.Owner);
        }
        Dirty(assistant, component);
        RefreshAssistantActions(suit, (assistant, component));
    }

    public bool IsAssistant(Entity<ModSuitComponent> suit, EntityUid user) =>
        !suit.Comp.Projected && suit.Comp.Assistant == user && suit.Comp.Powered
        && TryComp(user, out ModAssistantComponent? assistant) && assistant.Suit == suit.Owner
        && GetStoredAssistant(suit) == user
        && TryComp(user, out MindContainerComponent? mind) && mind.HasMind
        && !HasComp<StunnedComponent>(user)
        && !HasComp<AdminFrozenComponent>(user) && !HasComp<FrozenComponent>(user)
        && (!TryComp(user, out MobStateComponent? state) || state.CurrentState == MobState.Alive);

    private bool IsSuitOperator(Entity<ModSuitComponent> suit, EntityUid user) =>
        suit.Comp.Wearer == user || IsAssistant(suit, user);

    private void OnAssistantInteraction(Entity<ModAssistantComponent> assistant, ref InteractionAttemptEvent args)
    {
        if (!TryComp(assistant.Comp.Suit, out ModSuitComponent? control)
            || !IsAssistant((assistant.Comp.Suit, control), assistant))
            return;

        if (args.Target == null || args.Target == assistant.Comp.Suit
            || args.Target is { } target && control.ModulesContainer?.Contains(target) == true)
            args.Cancelled = false;
    }

    private void OnAssistantInterface(Entity<ModSuitComponent> suit, ref ModAssistantInterfaceEvent args)
    {
        if (!_net.IsClient && !args.Handled && IsAssistant(suit, args.Performer))
        {
            args.Handled = true;
            _ui.TryOpenUi(suit.Owner, ModSuitUiKey.Key, args.Performer);
        }
    }

    private void OnAssistantTargetOrigin(Entity<ModAssistantComponent> assistant, ref ActionTargetOriginEvent args)
    {
        if (!TryComp(assistant.Comp.Suit, out ModSuitComponent? suit) || !IsAssistant((assistant.Comp.Suit, suit), assistant)
            || !TryComp(args.Action, out ActionComponent? action) || action.AttachedEntity != assistant.Owner
            || action.Container is not { } module || suit.ModulesContainer?.Contains(module) != true
            || !TryComp(module, out ModModuleComponent? installed) || installed.Suit != assistant.Comp.Suit)
            return;

        args.Origin = suit.Wearer ?? assistant.Comp.Suit;
    }

    private void OnAssistantUiOpened(Entity<ModSuitComponent> suit, ref BoundUIOpenedEvent args)
    {
        if (!_net.IsClient && args.UiKey is ModSuitUiKey.Key
            && (!CanReachSuit(suit, args.Actor) || !CanAccessModules(suit, args.Actor)
                || !_actionBlocker.CanInteract(args.Actor, suit)))
            _ui.CloseUi(suit.Owner, ModSuitUiKey.Key, args.Actor);
    }

    private void OnAssistantMindRemoved(Entity<ModAssistantComponent> assistant, ref MindRemovedMessage args)
    {
        if (_net.IsClient)
            return;

        StopAssistantMovement(assistant);
        assistant.Comp.HeldButtons = MoveButtons.None;
        ClearAssistantActions(assistant);
    }

    private void OnAssistantMindAdded(Entity<ModAssistantComponent> assistant, ref MindAddedMessage args)
    {
        if (!_net.IsClient && TryComp(assistant.Comp.Suit, out ModSuitComponent? suit))
            RefreshAssistantActions((assistant.Comp.Suit, suit), assistant);
    }

    private void OnAssistantTerminating(Entity<ModAssistantComponent> assistant, ref EntityTerminatingEvent args)
    {
        if (!_net.IsClient)
        {
            StopAssistantMovement(assistant);
            ClearAssistantActions(assistant);
        }
    }

    private void ReleaseAssistant(Entity<ModSuitComponent> suit)
    {
        if (GetStoredAssistant(suit) is not { } assistant || TerminatingOrDeleted(assistant)
            || !_containers.TryGetContainingContainer(assistant, out var container))
            return;

        var coordinates = _transform.GetMoverCoordinates(suit);
        if (!_containers.Remove(assistant, container, force: true, destination: coordinates))
            return;

        if (HasComp<PAIComponent>(assistant))
            return;

        // TG preserves a destroyed suit's AI in a recoverable card, rather than deleting its mind.
        var card = SpawnAtPosition("Intellicard", coordinates);
        _assistantMobState.ChangeMobState(assistant, MobState.Dead);
        _itemSlots.TryInsert(card, StationAiHolderComponent.Container, assistant, null);
    }

    private void OnAssistantCanMove(Entity<ModAssistantMoverComponent> mover, ref UpdateCanMoveEvent args)
    {
        if (CanAssistantMove(mover.Comp.Assistant, mover.Owner))
            args.Uncancel();
    }

    private bool CanAssistantMove(EntityUid assistant, EntityUid target) =>
        TryComp(assistant, out ModAssistantComponent? helper)
        && TryComp(helper.Suit, out ModSuitComponent? control) && IsAssistant((helper.Suit, control), assistant)
        && helper.MovementTarget == target && IsAssistantTargetValid((helper.Suit, control), target);

    private bool IsAssistantTargetValid(Entity<ModSuitComponent> suit, EntityUid target) =>
        !TerminatingOrDeleted(target) && !Transform(target).Anchored
        && !HasComp<AdminFrozenComponent>(target) && !HasComp<FrozenComponent>(target)
        && !HasComp<BlockMovementComponent>(target)
        && TryComp(target, out PhysicsComponent? body) && body.BodyType != BodyType.Static
        && (!TryComp(target, out PullableComponent? pullable) || !pullable.BeingPulled)
        && (suit.Comp.Wearer is { } wearer
            ? wearer == target && suit.Comp.State == ModSuitState.Active
                && GetDeployedPart(suit, "shoes") is { } boots && Comp<ModPartComponent>(boots).Sealed
            : target == suit.Owner && !_containers.IsEntityInContainer(target) && !_gravity.IsWeightless(target));

    private void OnAssistantSpeed(Entity<ModAssistantMoverComponent> mover, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp(mover.Comp.Assistant, out ModAssistantComponent? helper)
            && TryComp(helper.Suit, out ModSuitComponent? control))
            args.ModifySpeed(control.Wearer == mover.Owner ? control.AssistantWornSpeedModifier : control.AssistantUnwornSpeedModifier);
    }

    private void OnAssistantModuleInstalled(Entity<ModModuleComponent> module, ref ModModuleInstalledEvent args) =>
        RefreshAssistantModule(args.Suit, module, true);

    private void OnAssistantModuleRemoved(Entity<ModModuleComponent> module, ref ModModuleRemovedEvent args) =>
        RefreshAssistantModule(args.Suit, module, false);

    private void RefreshAssistantModule(EntityUid uid, Entity<ModModuleComponent> module, bool installed)
    {
        if (_net.IsClient || !TryComp(uid, out ModSuitComponent? suit) || suit.Assistant is not { } assistant
            || !TryComp(assistant, out ModAssistantComponent? component))
            return;

        var changed = installed && module.Comp.Pinned
            ? component.PinnedModules.Add(module.Owner)
            : component.PinnedModules.Remove(module.Owner);
        if (changed)
            Dirty(assistant, component);
        RefreshAssistantActions((uid, suit), (assistant, component));
    }

    private void RefreshSuitAssistantActions(EntityUid uid)
    {
        if (!_net.IsClient && TryComp(uid, out ModSuitComponent? suit) && suit.Assistant is { } assistant
            && TryComp(assistant, out ModAssistantComponent? component))
            RefreshAssistantActions((uid, suit), (assistant, component));
    }

    private void RefreshAssistantActions(Entity<ModSuitComponent> suit, Entity<ModAssistantComponent> assistant)
    {
        if (!IsAssistant(suit, assistant))
        {
            ClearAssistantActions(assistant);
            return;
        }

        _actions.AddAction(assistant, ref assistant.Comp.InterfaceAction, AssistantInterfaceAction, suit);
        _actions.AddAction(assistant, ref assistant.Comp.ActivateAction, suit.Comp.ActivateAction, suit);
        _actions.AddAction(assistant, ref assistant.Comp.DeployAction, suit.Comp.DeployAction, suit);
        if (suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var (module, action) in assistant.Comp.ModuleActions.ToArray())
        {
            if (modules.Contains(module) && assistant.Comp.PinnedModules.Contains(module))
                continue;

            _actions.RemoveAction(assistant.Owner, action);
            QueueDel(action);
            assistant.Comp.ModuleActions.Remove(module);
        }

        foreach (var (module, action) in assistant.Comp.SelectorActions.ToArray())
        {
            if (modules.Contains(module) && HasComp<ModDeviceSelectorComponent>(module))
                continue;
            _actions.RemoveAction(assistant.Owner, action);
            QueueDel(action);
            assistant.Comp.SelectorActions.Remove(module);
        }

        foreach (var module in modules.ContainedEntities)
        {
            if (TryComp(module, out ModDeviceSelectorComponent? selector))
            {
                EntityUid? selectedAction = assistant.Comp.SelectorActions.TryGetValue(module, out var existing) ? existing : null;
                if (_actions.AddAction(assistant, ref selectedAction, selector.Action, module) && selectedAction is { } selected)
                {
                    assistant.Comp.SelectorActions[module] = selected;
                    _actions.SetEntityIcon(selected, module);
                }
            }
            if (!TryComp(module, out ModModuleComponent? installed) || !assistant.Comp.PinnedModules.Contains(module)
                || installed.Action is not { } prototype)
                continue;

            EntityUid? action = assistant.Comp.ModuleActions.GetValueOrDefault(module);
            if (action == EntityUid.Invalid)
                action = null;
            if (_actions.AddAction(assistant, ref action, prototype, module) && action is { } uid)
            {
                assistant.Comp.ModuleActions[module] = uid;
                // The module validates its operator; the pAI's ordinary world interactions remain blocked.
                _actions.SetCheckCanInteract(uid, false);
                _metadata.SetEntityName(uid, Name(module));
                _metadata.SetEntityDescription(uid, Description(module));
                _actions.SetEntityIcon(uid, module);
            }
            _actions.SetToggled(action, installed.Active);
        }
    }

    private void ClearAssistantActions(Entity<ModAssistantComponent> assistant)
    {
        _ui.CloseUi(assistant.Comp.Suit, ModSuitUiKey.Key, assistant);
        if (TryComp(assistant.Comp.Suit, out ModSuitComponent? control) && control.ModulesContainer is { } modules)
        {
            foreach (var module in modules.ContainedEntities)
            {
                CloseDeviceSelector(module, assistant);
                if (HasComp<ModStatusReadoutComponent>(module))
                    _ui.CloseUi(module, ModStatusReadoutUiKey.Key, assistant);
            }
        }
        _actions.RemoveProvidedActions(assistant, assistant.Comp.Suit);
        QueueDel(assistant.Comp.InterfaceAction);
        QueueDel(assistant.Comp.ActivateAction);
        QueueDel(assistant.Comp.DeployAction);
        foreach (var action in assistant.Comp.ModuleActions.Values)
        {
            _actions.RemoveAction(assistant.Owner, action);
            QueueDel(action);
        }
        assistant.Comp.ModuleActions.Clear();
        foreach (var action in assistant.Comp.SelectorActions.Values)
        {
            _actions.RemoveAction(assistant.Owner, action);
            QueueDel(action);
        }
        assistant.Comp.SelectorActions.Clear();
        assistant.Comp.InterfaceAction = null;
        assistant.Comp.ActivateAction = null;
        assistant.Comp.DeployAction = null;
    }

    private void OnAssistantInput(Entity<ModAssistantComponent> assistant, ref DirectionInputEvent args)
    {
        if (_net.IsClient || args.Source != assistant.Owner)
            return;

        foreach (var (button, direction) in AssistantDirections)
        {
            if (direction == args.Direction)
                assistant.Comp.HeldButtons = args.State ? assistant.Comp.HeldButtons | button : assistant.Comp.HeldButtons & ~button;
        }
        UpdateAssistantMovement(assistant);
    }

    private void OnAssistantDetached(Entity<ModAssistantComponent> assistant, ref PlayerDetachedEvent args)
    {
        if (!_net.IsClient)
        {
            assistant.Comp.HeldButtons = MoveButtons.None;
            StopAssistantMovement(assistant);
        }
    }

    private void OnAssistantWalkingInput(Entity<ModAssistantComponent> assistant, ref WalkingInputEvent args)
    {
        if (_net.IsClient || args.Source != assistant.Owner)
            return;

        assistant.Comp.HeldButtons = args.Walking
            ? assistant.Comp.HeldButtons | MoveButtons.Walk
            : assistant.Comp.HeldButtons & ~MoveButtons.Walk;
        UpdateAssistantMovement(assistant);
    }

    private void OnAssistantRelayChanged(Entity<ModAssistantComponent> assistant, ref EffectiveMoverChangedEvent args)
    {
        if (!_net.IsClient && assistant.Comp.MovementTarget is { } target && args.NewMover != target)
            StopAssistantMovement(assistant);
    }

    private void OnWearerDetached(Entity<ModAssistantMoverComponent> wearer, ref PlayerDetachedEvent args) =>
        ResetWearerInput(wearer);

    private void OnWearerAttached(Entity<ModAssistantMoverComponent> wearer, ref PlayerAttachedEvent args) =>
        ResetWearerInput(wearer);

    private void ResetWearerInput(Entity<ModAssistantMoverComponent> wearer)
    {
        if (_net.IsClient)
            return;
        wearer.Comp.OwnerButtons = MoveButtons.None;
        RestoreCombinedInput(wearer);
    }

    private void OnWearerMovementChanged(Entity<ModAssistantMoverComponent> wearer, ref MoveInputEvent args)
    {
        if (!_net.IsClient)
            RestoreCombinedInput(wearer);
    }

    private void RestoreCombinedInput(Entity<ModAssistantMoverComponent> wearer)
    {
        if (CanAssistantMove(wearer.Comp.Assistant, wearer.Owner)
            && TryComp(wearer.Comp.Assistant, out ModAssistantComponent? assistant)
            && TryComp(wearer, out InputMoverComponent? mover))
            _assistantMover.SetMoveInput((wearer.Owner, mover), wearer.Comp.OwnerButtons | assistant.HeldButtons);
    }

    private void OnWearerWalkingInput(Entity<ModAssistantMoverComponent> wearer, ref WalkingInputEvent args)
    {
        if (_net.IsClient || !TryComp(wearer.Comp.Assistant, out ModAssistantComponent? assistant))
            return;

        if (args.Source == wearer.Owner)
            wearer.Comp.OwnerButtons = args.Walking
                ? wearer.Comp.OwnerButtons | MoveButtons.Walk
                : wearer.Comp.OwnerButtons & ~MoveButtons.Walk;
        args.Walking = ((wearer.Comp.OwnerButtons | assistant.HeldButtons) & MoveButtons.Walk) != 0;
    }

    private void OnWearerInput(Entity<ModAssistantMoverComponent> wearer, ref DirectionInputEvent args)
    {
        if (_net.IsClient || !TryComp(wearer.Comp.Assistant, out ModAssistantComponent? assistant))
            return;

        foreach (var (button, direction) in AssistantDirections)
        {
            if (direction != args.Direction)
                continue;
            if (args.Source == wearer.Owner)
                wearer.Comp.OwnerButtons = args.State ? wearer.Comp.OwnerButtons | button : wearer.Comp.OwnerButtons & ~button;
            args.State = ((wearer.Comp.OwnerButtons | assistant.HeldButtons) & button) != 0;
            break;
        }
    }

    private void UpdateAssistants(float frameTime)
    {
        var query = EntityQueryEnumerator<ModAssistantComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var assistant = (uid, component);
            if (!TryComp(component.Suit, out ModSuitComponent? control) || TerminatingOrDeleted(component.Suit))
                continue;

            var suit = (component.Suit, control);
            UpdateAssistantMovement(assistant);
            if (component.MovementTarget != null && !TryUseModuleCharge(suit, control.AssistantMovePower * frameTime))
                StopAssistantMovement(assistant);
        }
    }

    private void UpdateAssistantMovement(Entity<ModAssistantComponent> assistant)
    {
        if (!TryComp(assistant.Comp.Suit, out ModSuitComponent? control)
            || !IsAssistant((assistant.Comp.Suit, control), assistant)
            || SharedMoverController.GetNormalizedMovement(assistant.Comp.HeldButtons & MoveButtons.AnyDirection) == MoveButtons.None
            || control.Wearer != null && control.State != ModSuitState.Active
            || control.Wearer == null && (_containers.IsEntityInContainer(assistant.Comp.Suit)
                || _gravity.IsWeightless(assistant.Comp.Suit)))
        {
            StopAssistantMovement(assistant);
            return;
        }

        var target = control.Wearer ?? assistant.Comp.Suit;
        if (!IsAssistantTargetValid((assistant.Comp.Suit, control), target))
        {
            StopAssistantMovement(assistant);
            return;
        }
        if (assistant.Comp.MovementTarget != target)
        {
            StopAssistantMovement(assistant);
            if (TryComp(target, out ModAssistantMoverComponent? occupied) && occupied.Assistant != assistant.Owner)
                return;
            if (TryComp(target, out MovementRelayTargetComponent? relayTarget) && Exists(relayTarget.Source)
                && relayTarget.Source != assistant.Owner)
                return;
            if (control.Wearer == null && TryComp(target, out PhysicsComponent? body) && body.BodyType != BodyType.KinematicController)
            {
                assistant.Comp.PreviousBodyType = body.BodyType;
                _assistantPhysics.SetBodyType(target, BodyType.KinematicController, body: body);
            }
            if (!TryComp(target, out InputMoverComponent? mover))
            {
                mover = AddComp<InputMoverComponent>(target);
                assistant.Comp.CreatedMover = mover;
            }
            assistant.Comp.MovementTarget = target;
            var motor = EnsureComp<ModAssistantMoverComponent>(target);
            motor.Assistant = assistant;
            motor.OwnerButtons = mover.HeldMoveButtons;
            if (!HasComp<MovementSpeedModifierComponent>(target))
                assistant.Comp.CreatedSpeedModifier = AddComp<MovementSpeedModifierComponent>(target);
            _actionBlocker.UpdateCanMove(target);
            _movement.RefreshMovementSpeedModifiers(target);
            _assistantMover.SetRelay(assistant, target);
            RestoreCombinedInput((target, motor));
        }

        if (!CanAssistantMove(assistant, target) || !HasComp<InputMoverComponent>(target))
        {
            StopAssistantMovement(assistant);
            return;
        }

        if (control.Wearer == target)
            _standing.Stand(target, force: true);
    }

    private void StopAssistantMovement(Entity<ModAssistantComponent> assistant)
    {
        if (assistant.Comp.MovementTarget is not { } target)
            return;

        assistant.Comp.MovementTarget = null;
        if (TryComp(assistant, out RelayInputMoverComponent? relay) && relay.RelayEntity == target
            && relay.LifeStage < ComponentLifeStage.Stopping)
            RemComp(assistant.Owner, relay);

        var restore = MoveButtons.None;
        if (TryComp(target, out ModAssistantMoverComponent? motor) && motor.Assistant == assistant.Owner)
        {
            restore = motor.OwnerButtons;
            RemComp(target, motor);
        }
        if (TryComp(target, out InputMoverComponent? mover))
        {
            _assistantMover.SetMoveInput((target, mover), restore);
            if (ReferenceEquals(mover, assistant.Comp.CreatedMover))
                RemComp(target, mover);
        }
        assistant.Comp.CreatedMover = null;
        if (TryComp(target, out MovementSpeedModifierComponent? speed) && ReferenceEquals(speed, assistant.Comp.CreatedSpeedModifier))
            RemComp(target, speed);
        assistant.Comp.CreatedSpeedModifier = null;
        if (assistant.Comp.PreviousBodyType is { } previous && TryComp(target, out PhysicsComponent? body)
            && body.BodyType == BodyType.KinematicController)
            _assistantPhysics.SetBodyType(target, previous, body: body);
        assistant.Comp.PreviousBodyType = null;
        _actionBlocker.UpdateCanMove(target);
        _movement.RefreshMovementSpeedModifiers(target);
        if (_assistantMobState.IsIncapacitated(target) || HasComp<StunnedComponent>(target))
            _standing.Down(target, playSound: false, dropHeldItems: false);
    }
}
