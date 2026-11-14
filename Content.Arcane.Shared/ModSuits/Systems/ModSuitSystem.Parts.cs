using System.Linq;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string PartsContainerId = "mod-parts";
    private const string StoredClothingId = "mod-part-stored";
    private const string OuterClothingSlot = "outerClothing";
    private const string SuitStorageSlot = "suitstorage";

    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private INetManager _net = default!;

    private readonly HashSet<EntityUid> _movingParts = [];

    private void InitializeParts()
    {
        SubscribeLocalEvent<ModSuitComponent, ComponentInit>(OnSuitInit);
        SubscribeLocalEvent<ModSuitComponent, MapInitEvent>(OnSuitMapInit);
        SubscribeLocalEvent<ModSuitComponent, ClothingGotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<ModSuitComponent, ClothingGotUnequippedEvent>(OnSuitUnequipped);
        SubscribeLocalEvent<ModSuitComponent, EntityTerminatingEvent>(OnSuitTerminating);
        SubscribeLocalEvent<InventoryComponent, InventoryTransferStartingEvent>(OnInventoryTransfer);
        SubscribeLocalEvent<ModPartComponent, ComponentInit>(OnPartInit);
        SubscribeLocalEvent<ModPartComponent, BeingUnequippedAttemptEvent>(OnPartUnequipAttempt);
        SubscribeLocalEvent<ModPartComponent, GotUnequippedEvent>(OnPartUnequipped);
        SubscribeLocalEvent<ModPartComponent, EntityTerminatingEvent>(OnPartTerminating);
    }

    private void OnSuitInit(Entity<ModSuitComponent> suit, ref ComponentInit args)
    {
        suit.Comp.PartsContainer = _containers.EnsureContainer<Container>(suit, PartsContainerId);
        suit.Comp.ModulesContainer = _containers.EnsureContainer<Container>(suit, ModulesContainerId);
    }

    private void OnPartInit(Entity<ModPartComponent> part, ref ComponentInit args) =>
        part.Comp.StoredClothing = _containers.EnsureContainer<ContainerSlot>(part, StoredClothingId);

    private void OnSuitMapInit(Entity<ModSuitComponent> suit, ref MapInitEvent args)
    {
        UpdatePower(suit);
        if (_net.IsClient || suit.Comp.PartsContainer is not { Count: 0 })
            return;

        foreach (var (slot, prototype) in suit.Comp.Parts)
        {
            if (!TrySpawnInContainer(prototype, suit, PartsContainerId, out var uid))
                continue;

            var part = EnsureComp<ModPartComponent>(uid.Value);
            part.Suit = suit;
            part.Slot = slot;
            Dirty(uid.Value, part);
            suit.Comp.PartEntities.Add(slot, uid.Value);
        }

        SpawnStartingModules(suit);
        Dirty(suit);
    }

    public bool IsPartDeployed(Entity<ModSuitComponent> suit, string slot) =>
        suit.Comp.Wearer is { } wearer
        && suit.Comp.PartEntities.TryGetValue(slot, out var part)
        && _inventory.TryGetSlotEntity(wearer, slot, out var equipped)
        && equipped == part;

    public bool TryDeployPart(Entity<ModSuitComponent> suit, string slot, EntityUid user)
    {
        if (_net.IsClient || suit.Comp.State != ModSuitState.Inactive || suit.Comp.Wearer != user
            || suit.Comp.PartsContainer is not { } parts
            || !suit.Comp.PartEntities.TryGetValue(slot, out var uid) || !parts.Contains(uid)
            || !TryComp(uid, out ModPartComponent? part)
            || part.StoredClothing is not { ContainedEntity: null } storage
            || !_actionBlocker.CanInteract(user, suit)
            || !_inventory.TryGetSlot(user, slot, out var slotDefinition)
            || _whitelist.IsWhitelistFail(slotDefinition.Whitelist, uid)
            || _whitelist.IsWhitelistPass(slotDefinition.Blacklist, uid)
            || !TryComp(uid, out ClothingComponent? clothing)
            || (clothing.Slots & slotDefinition.SlotFlags) == 0)
            return false;

        var suitStorage = StashSuitStorage(user, slot);
        try
        {
            if (_inventory.TryGetSlotEntity(user, slot, out var previous))
            {
                if (!_inventory.TryUnequip(user, slot, silent: true))
                    return false;

                if (!_containers.Insert(previous.Value, storage))
                {
                    RestoreClothing(user, previous.Value, slot);
                    return false;
                }
            }

            if (_inventory.TryEquip(user, uid, slot, silent: true, force: true, triggerHandContact: true))
                return true;

            RestoreStoredClothing(storage, user, slot);
            return false;
        }
        finally
        {
            if (suitStorage is { } saved)
                RestoreClothing(user, saved, SuitStorageSlot);
        }
    }

    public bool TryRetractPart(Entity<ModSuitComponent> suit, string slot, EntityUid user) =>
        !_net.IsClient && suit.Comp.State == ModSuitState.Inactive && suit.Comp.Wearer == user
        && _actionBlocker.CanInteract(user, suit)
        && RetractPart(suit, slot, user);

    private bool RetractPart(Entity<ModSuitComponent> suit, string slot, EntityUid wearer)
    {
        if (suit.Comp.PartsContainer is not { } parts || !IsPartDeployed(suit, slot)
            || !suit.Comp.PartEntities.TryGetValue(slot, out var uid)
            || !TryComp(uid, out ModPartComponent? part) || !_movingParts.Add(uid))
            return false;

        var suitStorage = StashSuitStorage(wearer, slot);
        try
        {
            if (!_inventory.TryUnequip(wearer, slot, silent: true, force: true, triggerHandContact: true))
                return false;

            if (!_containers.Insert(uid, parts))
            {
                RestoreClothing(wearer, uid, slot);
                return false;
            }

            if (part.StoredClothing is { } storage)
                RestoreStoredClothing(storage, wearer, slot);
            return true;
        }
        finally
        {
            _movingParts.Remove(uid);
            if (suitStorage is { } saved)
                RestoreClothing(wearer, saved, SuitStorageSlot);
        }
    }

    private EntityUid? StashSuitStorage(EntityUid wearer, string slot)
    {
        return slot == OuterClothingSlot
            && _inventory.TryUnequip(wearer, SuitStorageSlot, out var saved, silent: true, force: true)
            ? saved
            : null;
    }

    private void RestoreStoredClothing(ContainerSlot container, EntityUid wearer, string slot)
    {
        // Ordinary removal can move the clothing into an enclosing MOD compartment.
        if (container.ContainedEntity is { } saved && !TerminatingOrDeleted(wearer)
            && _containers.Remove(saved, container, force: true, destination: Transform(wearer).Coordinates))
            RestoreClothing(wearer, saved, slot);
    }

    private void RestoreClothing(EntityUid wearer, EntityUid item, string slot)
    {
        if (TerminatingOrDeleted(item) || TerminatingOrDeleted(wearer))
            return;

        if (!_inventory.TryEquip(wearer, item, slot, silent: true, triggerHandContact: true))
            _hands.PickupOrDrop(wearer, item);
    }

    private void OnSuitEquipped(Entity<ModSuitComponent> suit, ref ClothingGotEquippedEvent args)
    {
        if (_net.IsClient || args.Clothing.InSlotFlag is not { } slot || (slot & SlotFlags.BACK) == 0)
            return;

        suit.Comp.Wearer = args.Wearer;
        RefreshEquipmentGrants(suit);
        UpdateModuleWearer(suit, args.Wearer, equipped: true);
        RefreshWearerPower(suit);
        Dirty(suit);
    }

    private void OnSuitUnequipped(Entity<ModSuitComponent> suit, ref ClothingGotUnequippedEvent args)
    {
        if (_net.IsClient || suit.Comp.Wearer != args.Wearer || TerminatingOrDeleted(suit)
            || TerminatingOrDeleted(args.Wearer))
            return;

        CancelDeployment(suit);
        StopActivation(suit);
        RetractAllParts(suit, args.Wearer);
        UpdateModuleWearer(suit, args.Wearer, equipped: false);
        ClearPowerAlert(suit, args.Wearer);
        suit.Comp.Wearer = null;
        RefreshEquipmentGrants(suit);
        Dirty(suit);
    }

    private void OnInventoryTransfer(Entity<InventoryComponent> source, ref InventoryTransferStartingEvent args)
    {
        if (!_inventory.TryGetSlotEntity(source, "back", out var item, source.Comp)
            || !TryComp(item, out ModSuitComponent? suit) || suit.Wearer != source.Owner)
            return;

        StopActivation((item.Value, suit));
        CancelDeployment((item.Value, suit));
        RetractAllParts((item.Value, suit), source);
    }

    private void RetractAllParts(Entity<ModSuitComponent> suit, EntityUid wearer)
    {
        foreach (var slot in suit.Comp.PartEntities.Keys)
            RetractPart(suit, slot, wearer);
    }

    private void OnPartUnequipAttempt(Entity<ModPartComponent> part, ref BeingUnequippedAttemptEvent args)
    {
        if (part.Comp.Suit != null && !_movingParts.Contains(part))
        {
            args.Reason = "modsuit-part-retract-instead";
            args.Cancel();
        }
    }

    private void OnPartUnequipped(Entity<ModPartComponent> part, ref GotUnequippedEvent args)
    {
        if (_net.IsClient || _movingParts.Contains(part) || TerminatingOrDeleted(part)
            || part.Comp.Suit is not { } uid || TerminatingOrDeleted(uid)
            || !TryComp(uid, out ModSuitComponent? suit) || suit.PartsContainer is not { } parts
            || args.EquipTarget != suit.Wearer || args.Slot != part.Comp.Slot)
            return;

        // The inventory still has to clear the old clothing state and its dependent slots.
        var wearer = args.EquipTarget;
        var slot = args.Slot;
        Timer.Spawn(0, () =>
        {
            if (TerminatingOrDeleted(part) || TerminatingOrDeleted(uid) || TerminatingOrDeleted(wearer)
                || IsPartDeployed((uid, suit), slot))
                return;

            if (!_containers.IsEntityInContainer(part))
                _containers.Insert(part.Owner, parts);

            if (suit.Wearer == wearer)
                StopActivation((uid, suit));
            if (part.Comp.StoredClothing is { } storage)
                RestoreStoredClothing(storage, wearer, slot);
        });
    }

    private void OnPartTerminating(Entity<ModPartComponent> part, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (part.Comp.Suit is { } uid && !TerminatingOrDeleted(uid) && TryComp(uid, out ModSuitComponent? suit))
            StopActivation((uid, suit));

        if (part.Comp.StoredClothing is { ContainedEntity: { } saved } storage && !TerminatingOrDeleted(saved))
            _containers.Remove(saved, storage, force: true, destination: _transform.GetMoverCoordinates(part));
    }

    private void OnSuitTerminating(Entity<ModSuitComponent> suit, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (suit.Comp.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            ClearPowerAlert(suit, wearer);

        if (suit.Comp.ModulesContainer is { } modules)
        {
            foreach (var uid in modules.ContainedEntities.ToArray())
            {
                if (TryComp(uid, out ModModuleComponent? module))
                    DetachModule(suit, (uid, module));
            }
        }

        foreach (var part in suit.Comp.PartEntities.Values)
        {
            if (!TerminatingOrDeleted(part) && suit.Comp.PartsContainer?.Contains(part) != true)
                QueueDel(part);
        }
    }
}
