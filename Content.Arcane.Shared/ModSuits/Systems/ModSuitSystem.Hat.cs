// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedItemSystem _modItem = default!;

    private void InitializeHat()
    {
        SubscribeLocalEvent<ModHatStabilizerModuleComponent, ModModuleInstalledEvent>(OnHatModuleInstalled);
        SubscribeLocalEvent<ModHatStabilizerModuleComponent, ModModuleRemovedEvent>(OnHatModuleRemoved);
        SubscribeLocalEvent<ModHatStabilizerModuleComponent, EntityTerminatingEvent>(OnHatModuleTerminating);
        SubscribeLocalEvent<ModHatHolderComponent, ItemSlotInsertAttemptEvent>(OnHatInsertAttempt);
        SubscribeLocalEvent<ModHatHolderComponent, EntInsertedIntoContainerMessage>(OnHatInserted);
        SubscribeLocalEvent<ModHatHolderComponent, EntRemovedFromContainerMessage>(OnHatRemoved);
        SubscribeLocalEvent<ModHatHolderComponent, EntityTerminatingEvent>(OnHatHelmetTerminating);
    }

    private void OnHatModuleInstalled(Entity<ModHatStabilizerModuleComponent> module, ref ModModuleInstalledEvent args)
    {
        if (_net.IsClient || !TryComp(args.Suit, out ModSuitComponent? suit)
            || !suit.PartEntities.TryGetValue("head", out var helmet) || TerminatingOrDeleted(helmet)
            || HasComp<ModHatHolderComponent>(helmet))
            return;

        _itemSlots.AddItemSlot(helmet, ModHatHolderComponent.SlotId, new ItemSlot
        {
            Name = "modsuit-hat-slot",
            Swap = false,
        });
        AddComp<ModHatHolderComponent>(helmet);
        module.Comp.Helmet = helmet;
    }

    private void OnHatModuleRemoved(Entity<ModHatStabilizerModuleComponent> module, ref ModModuleRemovedEvent args) =>
        ReleaseHatModule(module);

    private void OnHatModuleTerminating(Entity<ModHatStabilizerModuleComponent> module, ref EntityTerminatingEvent args) =>
        ReleaseHatModule(module);

    private void ReleaseHatModule(Entity<ModHatStabilizerModuleComponent> module)
    {
        if (_net.IsClient || module.Comp.Helmet is not { } helmet)
            return;

        module.Comp.Helmet = null;
        if (TerminatingOrDeleted(helmet) || !_itemSlots.TryGetSlot(helmet, ModHatHolderComponent.SlotId, out var slot))
            return;

        EjectHat(helmet, slot);
        _itemSlots.RemoveItemSlot(helmet, slot);
        RemComp<ModHatHolderComponent>(helmet);
        _modItem.VisualsChanged(helmet);
    }

    private void OnHatInsertAttempt(Entity<ModHatHolderComponent> helmet, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID == ModHatHolderComponent.SlotId
            && (!TryComp(args.Item, out ClothingComponent? clothing) || !clothing.Slots.HasFlag(SlotFlags.HEAD)
                || HasComp<ModPartComponent>(args.Item) || HasComp<ModHatHolderComponent>(args.Item)))
            args.Cancelled = true;
    }

    private void OnHatInserted(Entity<ModHatHolderComponent> helmet, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ModHatHolderComponent.SlotId)
            _modItem.VisualsChanged(helmet);
    }

    private void OnHatRemoved(Entity<ModHatHolderComponent> helmet, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ModHatHolderComponent.SlotId)
            _modItem.VisualsChanged(helmet);
    }

    private void OnHatHelmetTerminating(Entity<ModHatHolderComponent> helmet, ref EntityTerminatingEvent args)
    {
        if (_net.IsServer && _itemSlots.TryGetSlot(helmet.Owner, ModHatHolderComponent.SlotId, out var slot))
            EjectHat(helmet, slot);
    }

    private void EjectHat(EntityUid helmet, ItemSlot slot)
    {
        if (slot.ContainerSlot is not { ContainedEntity: { } hat } container || TerminatingOrDeleted(hat))
            return;

        var destination = _transform.GetMoverCoordinates(helmet);
        if (destination.EntityId != helmet && destination.IsValid(EntityManager))
            _containers.Remove(hat, container, force: true, destination: destination);
        else
            _transform.DetachEntity(hat);
    }
}
