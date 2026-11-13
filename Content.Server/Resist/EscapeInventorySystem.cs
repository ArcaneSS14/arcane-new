using Content.Server.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Resist;
using Content.Shared.Storage;
using Content.Trauma.Common.Carrying; // Arcane
using Robust.Shared.Containers;

namespace Content.Server.Resist;

public sealed partial class EscapeInventorySystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private CommonCarryingSystem _carrying = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        // Arcane: a carried mob is parented to its carrier, outside an inventory container.
        if (_carrying.GetCarrier(uid) is { } carrier)
        {
            if (_actionBlockerSystem.CanInteract(uid, carrier))
                AttemptEscape(uid, carrier, component, breakOnMove: false);
            return;
        }

        if (!_containerSystem.TryGetContainingContainer((uid, null, null), out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued)
        if (!_containerSystem.CanRemove(uid, container))
        {
            _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), uid, uid);
            return;
        }

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out _))
        {
            AttemptEscape(uid, container.Owner, component);
            return;
        }

        // Uncontested
        if (HasComp<StorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SecretStashComponent>(container.Owner))
            AttemptEscape(uid, container.Owner, component);
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f,
        bool breakOnMove = true) // Arcane
    {
        if (component.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.BaseResistTime * multiplier, new EscapeInventoryEvent(), user, target: container)
        {
            CancelDuplicate = false, // Trauma
            BreakOnMove = breakOnMove,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out component.DoAfter))
            return;

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);

        _actions.AddAction(user, ref component.EscapeCancelAction, _escapeCancelAction); // Trauma
    }

    private void OnEscape(EntityUid uid, CanEscapeInventoryComponent component, EscapeInventoryEvent args)
    {
        // <Trauma>
        _actions.RemoveAction(uid, component.EscapeCancelAction);
        component.EscapeCancelAction = null;
        // </Trauma>
        component.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        // Arcane: the carrying system handles completion and cleans up both ends.
        if (_carrying.GetCarrier(uid) != null || args.Args.Target is not { } original ||
            !_containerSystem.TryGetContainingContainer((uid, null, null), out var current) || current.Owner != original)
            return;

        _containerSystem.AttachParentToContainerOrGrid((uid, Transform(uid)));
        args.Handled = true;
    }

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        if (component.DoAfter != null)
            _doAfterSystem.Cancel(component.DoAfter);
    }
}
