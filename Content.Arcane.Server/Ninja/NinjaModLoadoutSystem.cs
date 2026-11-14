using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Server.Ninja.Events;
using Content.Shared.Inventory;
using Content.Shared.Ninja.Components;
using Content.Shared.Roles;

namespace Content.Arcane.Server.Ninja;

public sealed partial class NinjaModLoadoutSystem : EntitySystem
{
    private static readonly ProtoId<StartingGearPrototype> NinjaModGear = "SpaceNinjaModGear";

    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ModSuitSystem _suits = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpaceNinjaComponent, StartingGearEquippedEvent>(OnStartingGearEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, ModSuitBatteryChangedEvent>(OnBatteryChanged);
    }

    private void OnBatteryChanged(Entity<NinjaSuitComponent> suit, ref ModSuitBatteryChangedEvent args)
    {
        var changed = new NinjaBatteryChangedEvent(args.Battery, suit.Owner);
        RaiseLocalEvent(suit.Owner, ref changed);
        RaiseLocalEvent(args.Wearer, ref changed);
    }

    private void OnStartingGearEquipped(Entity<SpaceNinjaComponent> ninja, ref StartingGearEquippedEvent args)
    {
        if (args.StartingGear is not StartingGearPrototype gear || gear.ID != NinjaModGear.Id)
            return;

        // The chestplate must open suit storage before the species' EVA loadout supplies the tank.
        if (!_inventory.TryGetSlotEntity(ninja, "back", out var back)
            || !TryComp<ModSuitComponent>(back, out var suit)
            || !_suits.TryPrepareStartingSuit((back.Value, suit), ninja))
            Log.Error($"Failed to prepare starting MOD for {ToPrettyString(ninja)}.");
    }
}
