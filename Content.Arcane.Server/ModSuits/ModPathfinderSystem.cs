using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.ModSuits;

public sealed partial class ModPathfinderSystem : EntitySystem
{
    [Dependency] private ModSuitSystem _suits = default!;
    [Dependency] private SharedImplanterSystem _implanter = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModPathfinderComponent, EntInsertedIntoContainerMessage>(OnImplantInserted);
        SubscribeLocalEvent<ModPathfinderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModPathfinderComponent, ModModuleUsedEvent>(OnUsed);
        SubscribeLocalEvent<ModPathfinderComponent, ModModuleRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<ModPathfinderComponent, ModSuitAccessAttemptEvent>(OnAccess);
        SubscribeLocalEvent<ModPathfinderComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ModPathfinderImplantComponent, ModPathfinderRecallEvent>(OnRecall);
        SubscribeLocalEvent<ModPathfinderFlightComponent, ComponentShutdown>(OnFlightShutdown);
    }

    private void OnMapInit(Entity<ModPathfinderComponent> ent, ref MapInitEvent args)
    {
        if (_slots.GetItemOrNull(ent.Owner, ImplanterComponent.ImplanterSlotId) is { } implant)
            Link(ent, implant);
    }

    private void OnImplantInserted(Entity<ModPathfinderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ImplanterComponent.ImplanterSlotId)
            Link(ent, args.Entity);
    }

    private void Link(Entity<ModPathfinderComponent> module, EntityUid implant)
    {
        if (TryComp<ModPathfinderImplantComponent>(implant, out var link)
            && (link.Module == null || link.Module == module.Owner))
        {
            module.Comp.Implant = implant;
            link.Module = module;
        }
    }

    private void OnUsed(Entity<ModPathfinderComponent> ent, ref ModModuleUsedEvent args)
    {
        if (TryComp<ImplanterComponent>(ent, out var implanter)
            && _implanter.CanImplant(args.User, args.User, (ent, implanter), out _, out _))
        {
            _implanter.Implant(args.User, args.User, (ent, implanter));
            args.Handled = true;
        }
    }

    private void OnRecall(Entity<ModPathfinderImplantComponent> ent, ref ModPathfinderRecallEvent args)
    {
        if (!args.Handled)
            args.Handled = TryRecall(ent, args.Performer);
    }

    public bool TryRecall(Entity<ModPathfinderImplantComponent> implant, EntityUid user)
    {
        if (!TryComp<SubdermalImplantComponent>(implant, out var implanted) || implanted.ImplantedEntity != user
            || !TryComp<ModPathfinderComponent>(implant.Comp.Module, out var module)
            || module.Implant != implant.Owner || module.Flight != null || _timing.CurTime < module.NextRecall
            || !_blocker.CanConsciouslyPerformAction(user) || !TryGetReadySuit(implant.Comp.Module.Value, user, out var suit))
            return false;

        var now = _timing.CurTime;
        var flight = AddComp<ModPathfinderFlightComponent>(suit);
        flight.Module = implant.Comp.Module.Value;
        flight.Target = user;
        flight.LaunchAt = now + module.Warmup;
        flight.ArriveAt = flight.LaunchAt + module.FlightTime;
        module.Flight = suit;
        module.NextRecall = now + module.Cooldown;
        _popup.PopupEntity(Loc.GetString("modsuit-pathfinder-incoming"), user, user);
        return true;
    }

    private bool TryGetReadySuit(EntityUid moduleUid, EntityUid user, out Entity<ModSuitComponent> suit)
    {
        if (!_suits.TryGetInstalledModuleSuit(moduleUid, out suit) || suit.Comp.Wearer != null
            || TerminatingOrDeleted(suit) || suit.Comp.Projected || suit.Comp.State != ModSuitState.Inactive
            || HasComp<WiresPanelComponent>(suit) && Comp<WiresPanelComponent>(suit).Open
            || !TryComp<ModPathfinderComponent>(moduleUid, out var module)
            || _suits.GetBattery(suit) is not { } battery || _battery.GetCharge(battery.Owner).Charge < module.ChargeCost
            || TerminatingOrDeleted(user))
            return false;

        var from = _transform.GetMapCoordinates(suit);
        var to = _transform.GetMapCoordinates(user);
        if (from.MapId == MapId.Nullspace || from.MapId != to.MapId)
            return false;

        var parent = Transform(suit).ParentUid;
        while (parent != EntityUid.Invalid)
        {
            if (HasComp<MobStateComponent>(parent) || HasComp<HandsComponent>(parent))
                return false;
            parent = Transform(parent).ParentUid;
        }
        return true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ModPathfinderFlightComponent>();
        while (query.MoveNext(out var uid, out var flight))
        {
            if (!TryComp<ModPathfinderComponent>(flight.Module, out var module)
                || !TryComp<ModPathfinderImplantComponent>(module.Implant, out var link)
                || link.Module != flight.Module
                || !TryComp<SubdermalImplantComponent>(module.Implant, out var implant)
                || implant.ImplantedEntity != flight.Target
                || !TryGetReadySuit(flight.Module, flight.Target, out var suit) || suit.Owner != uid)
            {
                RemComp<ModPathfinderFlightComponent>(uid);
                continue;
            }

            var now = _timing.CurTime;
            if (now < flight.LaunchAt)
                continue;

            if (!flight.Launched && !Launch((uid, flight)))
            {
                RemComp<ModPathfinderFlightComponent>(uid);
                continue;
            }

            var destination = _transform.GetMapCoordinates(flight.Target);
            var fraction = module.FlightTime > TimeSpan.Zero
                ? Math.Clamp((float) ((now - flight.LaunchAt) / module.FlightTime), 0f, 1f) : 1f;
            _transform.SetMapCoordinates(uid, new MapCoordinates(Vector2.Lerp(flight.Origin.Position, destination.Position, fraction), destination.MapId));
            if (now < flight.ArriveAt)
                continue;

            var target = flight.Target;
            var slot = module.SuitSlot;
            var charge = module.ChargeCost;
            flight.Arrived = true;
            RemComp<ModPathfinderFlightComponent>(uid);
            if (_inventory.TryGetSlotEntity(target, slot, out _) && !_inventory.TryUnequip(target, slot))
                continue;
            if (_suits.GetBattery(suit) is not { } battery || !_battery.TryUseCharge(battery.AsNullable(), charge))
                continue;
            if (_inventory.TryEquip(target, uid, slot))
                _suits.TryToggleAllParts(suit, target);
        }
    }

    private bool Launch(Entity<ModPathfinderFlightComponent> ent)
    {
        if (_containers.TryGetContainingContainer(ent.Owner, out var container)
            && (!_transform.TryGetMapOrGridCoordinates(ent.Owner, out var position)
                || !_containers.Remove(ent.Owner, container, destination: position.Value)))
            return false;

        ent.Comp.Origin = _transform.GetMapCoordinates(ent.Owner);
        ent.Comp.Launched = true;
        if (TryComp<PhysicsComponent>(ent, out var physics))
        {
            ent.Comp.RestoreCollision = physics.CanCollide;
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
            _physics.SetCanCollide(ent, false, body: physics);
        }
        return true;
    }

    private void OnFlightShutdown(Entity<ModPathfinderFlightComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<ModPathfinderComponent>(ent.Comp.Module, out var module) && module.Flight == ent.Owner)
            module.Flight = null;

        if (TerminatingOrDeleted(ent) || !ent.Comp.Launched)
            return;

        if (!ent.Comp.Arrived && _transform.GetMapCoordinates(ent.Owner).MapId == ent.Comp.Origin.MapId)
            _transform.SetMapCoordinates(ent.Owner, ent.Comp.Origin);
        if (ent.Comp.RestoreCollision)
            _physics.SetCanCollide(ent, true);
    }

    private void OnRemoved(Entity<ModPathfinderComponent> ent, ref ModModuleRemovedEvent args)
    {
        if (ent.Comp.Flight is { } flight)
            RemComp<ModPathfinderFlightComponent>(flight);
    }

    private void OnAccess(Entity<ModPathfinderComponent> ent, ref ModSuitAccessAttemptEvent args)
    {
        if (ent.Comp.Flight != null)
            args.Cancelled = true;
    }

    private void OnTerminating(Entity<ModPathfinderComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Flight is { } flight)
            RemComp<ModPathfinderFlightComponent>(flight);
        if (ent.Comp.Implant is { } implant && !TerminatingOrDeleted(implant))
            QueueDel(implant);
    }
}
