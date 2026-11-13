using Content.Shared.Hands.Components;
using Content.Shared.Inventory; // Arcane
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Hands.EntitySystems;

public sealed partial class ExtraHandsEquipmentSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ExtraHandsEquipmentComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked as part of the same game state.

        if (!TryComp<HandsComponent>(args.EquipTarget, out var handsComp))
            return;

        AddEquipmentHands((args.EquipTarget, handsComp), ent); // Arcane
    }

    // Arcane-Start: rebuild only hands from equipment that is still worn.
    public HashSet<string> GetEquipmentHandIds(EntityUid body)
    {
        var ids = new HashSet<string>();
        if (!_inventory.TryGetContainerSlotEnumerator(body, out var slots))
            return ids;

        while (slots.NextItem(out var item, out _))
        {
            if (TryComp<ExtraHandsEquipmentComponent>(item, out var extra))
            {
                foreach (var name in extra.Hands.Keys)
                    ids.Add(GetEquipmentHandId(item, name));
            }
        }
        return ids;
    }

    public void RestoreEquipmentHands(Entity<HandsComponent> body)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(body.Owner, out var slots))
            return;

        while (slots.NextItem(out var item, out _))
        {
            if (TryComp<ExtraHandsEquipmentComponent>(item, out var extra))
                AddEquipmentHands(body, (item, extra));
        }
    }

    private void AddEquipmentHands(Entity<HandsComponent> body, Entity<ExtraHandsEquipmentComponent> equipment)
    {
        foreach (var (name, hand) in equipment.Comp.Hands)
            _hands.AddHand(body.AsNullable(), GetEquipmentHandId(equipment.Owner, name), hand.Location);
    }

    private string GetEquipmentHandId(EntityUid item, string name) => $"{GetNetEntity(item).Id}-{name}";
    // Arcane-End

    private void OnUnequipped(Entity<ExtraHandsEquipmentComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked as part of the same game state.

        if (!TryComp<HandsComponent>(args.EquipTarget, out var handsComp))
            return;

        foreach (var handName in ent.Comp.Hands.Keys)
        {
            // add the NetEntity id to the container name to prevent multiple items with this component from conflicting
            var handId = GetEquipmentHandId(ent.Owner, handName); // Arcane
            _hands.RemoveHand((args.EquipTarget, handsComp), handId);
        }
    }
}
