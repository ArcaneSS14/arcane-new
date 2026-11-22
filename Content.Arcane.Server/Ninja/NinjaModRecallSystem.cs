using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Weapons.EnergyKatanaSheath;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Arcane.Server.Ninja;

public sealed partial class NinjaModRecallSystem : EntitySystem
{
    private const float VisibleRange = 7f;
    private const float RecallCharge = 3.6f;
    private static readonly EntProtoId EnergyKatana = "EnergyKatana";

    [Dependency] private ModSuitSystem _mods = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private ThrownItemSystem _thrown = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NinjaModRecallComponent, ModModuleUsedEvent>(OnRecall);
        SubscribeLocalEvent<SpaceNinjaComponent, ThrowHitByEvent>(OnCatch);
        SubscribeLocalEvent<NinjaKatanaReturningComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnRecall(Entity<NinjaModRecallComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || !TryComp<SpaceNinjaComponent>(args.User, out var ninja)
            || !_mods.TryGetInstalledModuleSuit(module.Owner, out var suit) || !CanRecall(suit, args.User))
            return;

        var user = args.User;
        if (ninja.Katana is not { } katana || TerminatingOrDeleted(katana))
        {
            foreach (var item in _hands.EnumerateHeld(user))
            {
                if (MetaData(item).EntityPrototype?.ID != EnergyKatana.Id)
                    continue;

                var bind = new BindItemEvent(item, ReplaceDeleted: true);
                RaiseLocalEvent(user, ref bind);
                _popup.PopupEntity(Loc.GetString("modsuit-ninja-katana-linked"), user, user);
                args.Handled = true;
                return;
            }
            return;
        }

        if (_transform.IsParentOf(Transform(user), katana) || HasComp<NinjaKatanaReturningComponent>(katana))
            return;

        var origin = _transform.GetMapCoordinates(katana);
        var destination = _transform.GetMapCoordinates(user);
        if (origin.MapId != destination.MapId || origin.MapId == MapId.Nullspace)
            return;

        var distance = (destination.Position - origin.Position).Length();
        if (distance > 0.75f && _containers.IsInSameOrParentContainer(user, katana)
            && _interaction.InRangeUnobstructed(user, katana, VisibleRange))
        {
            if (!ReleaseFromContainer(katana, user)
                || !_throwing.TryThrow(katana, (destination.Position - origin.Position).Normalized() * (distance + 0.5f),
                    pushbackRatio: 0, compensateFriction: true, recoil: false, doSpin: false))
                return;

            var returning = EnsureComp<NinjaKatanaReturningComponent>(katana);
            returning.Module = module;
            returning.Wearer = user;
            args.Handled = true;
            return;
        }

        var battery = _mods.GetBattery(suit);
        var charge = distance * RecallCharge;
        if (battery == null || _battery.GetCharge(battery.Value.Owner).Charge < charge)
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (!ReleaseFromContainer(katana, user))
            return;

        _battery.TryUseCharge(battery.Value.AsNullable(), charge);
        Receive(user, katana);
        args.Handled = true;
    }

    private bool CanRecall(Entity<ModSuitComponent> suit, EntityUid user) =>
        suit.Comp.Wearer == user && suit.Comp.State == ModSuitState.Active && suit.Comp.Powered
        && _mods.IsPartDeployed(suit, "gloves");

    private bool ReleaseFromContainer(EntityUid weapon, EntityUid user)
    {
        if (!_containers.TryGetContainingContainer(weapon, out var container))
            return true;

        if (_slots.TryGetSlot(container.Owner, container.ID, out var slot))
            return _slots.TryEject(container.Owner, slot, user, out _, doAfter: false);

        return _containers.Remove(weapon, container);
    }

    private void OnCatch(Entity<SpaceNinjaComponent> ninja, ref ThrowHitByEvent args)
    {
        if (args.Handled || !TryComp<NinjaKatanaReturningComponent>(args.Thrown, out var returning)
            || returning.Wearer != ninja.Owner || ninja.Comp.Katana != args.Thrown
            || !_mods.TryGetInstalledModuleSuit(returning.Module, out var suit) || !CanRecall(suit, ninja))
            return;

        var weapon = args.Thrown;
        _thrown.StopThrow(weapon, args.Component);
        Receive(ninja, weapon);
        args.Handled = true;
    }

    private void OnStopThrow(Entity<NinjaKatanaReturningComponent> weapon, ref StopThrowEvent args) =>
        RemComp(weapon, weapon.Comp);

    private void Receive(EntityUid user, EntityUid weapon)
    {
        _physics.SetLinearVelocity(weapon, Vector2.Zero);
        _transform.SetCoordinates(weapon, _transform.GetMoverCoordinates(user));
        if (_hands.TryPickupAnyHand(user, weapon))
            return;

        if (_inventory.TryGetSlotEntity(user, "belt", out var belt)
            && TryComp<EnergyKatanaSheathComponent>(belt, out var sheath)
            && _slots.TryInsert(belt.Value, sheath.Slot, weapon, user))
            return;

        _inventory.TryEquip(user, weapon, "belt", silent: true);
    }
}
