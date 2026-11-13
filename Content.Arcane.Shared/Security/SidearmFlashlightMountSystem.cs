using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.Security;

public sealed partial class SidearmFlashlightMountSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SidearmFlashlightMountComponent> ent, ref ComponentInit args)
    {
        // Weapon variants can replace the inherited YAML magazine or cell slot dictionary.
        if (!_slots.TryGetSlot(ent.Owner, SidearmFlashlightMountComponent.SlotId, out _))
            _slots.AddItemSlot(ent.Owner, SidearmFlashlightMountComponent.SlotId, ent.Comp.Slot);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<SidearmFlashlightMountComponent> ent, ref ComponentRemove args)
    {
        _slots.RemoveItemSlot(ent.Owner, ent.Comp.Slot);
    }

    [SubscribeLocalEvent]
    private void OnFlashlightInserted(Entity<SidearmFlashlightMountComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != SidearmFlashlightMountComponent.SlotId ||
            GetEquippedBy(ent) is not { } holder)
            return;

        var actions = new GetItemActionsEvent(_actionContainer, holder, args.Entity);
        RaiseLocalEvent(args.Entity, actions);
        if (actions.Actions.Count == 0)
            return;

        _actions.GrantActions(holder, actions.Actions, args.Entity);
        _actions.LoadActions(holder);
    }

    [SubscribeLocalEvent]
    private void OnFlashlightRemoved(Entity<SidearmFlashlightMountComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != SidearmFlashlightMountComponent.SlotId ||
            GetEquippedBy(ent) is not { } holder)
            return;

        _actions.RemoveProvidedActions(holder, args.Entity);
        _actions.SaveActions(holder);
    }

    private EntityUid? GetEquippedBy(EntityUid weapon)
    {
        if (_hands.IsHeld(weapon, out var holder))
            return holder;

        return _inventory.TryGetContainingSlot(weapon, out _) ? Transform(weapon).ParentUid : null;
    }
}
