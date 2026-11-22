using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Alert;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;

    private static readonly ProtoId<AlertPrototype> PowerAlert = "ModsuitPower";

    public override void Initialize()
    {
        SubscribeLocalEvent<ModCoreComponent, RefreshChargeRateEvent>(OnCoreRefreshChargeRate);
        SubscribeLocalEvent<ModCoreComponent, BatteryStateChangedEvent>(OnCoreBatteryStateChanged);
        SubscribeLocalEvent<ModCoreComponent, PowerCellChangedEvent>(OnCoreCellChanged);
        SubscribeLocalEvent<ModSuitComponent, FindBatteryEvent>(OnFindBattery);
        SubscribeLocalEvent<ModSuitComponent, InventoryRelayedEvent<FindBatteryEvent>>(OnFindWornBattery);
        SubscribeLocalEvent<ModSuitComponent, EntInsertedIntoContainerMessage>(OnSuitItemInserted);
        SubscribeLocalEvent<ModSuitComponent, EntRemovedFromContainerMessage>(OnSuitItemRemoved);
        SubscribeLocalEvent<ModSuitComponent, ItemToggleActivateAttemptEvent>(OnNinjaCloakAttempt);
        SubscribeLocalEvent<ModSuitComponent, ItemToggledEvent>(OnNinjaCloakToggled);
        SubscribeLocalEvent<ModSuitComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshSuitSpeed);
        InitializeParts();
        InitializeActivation();
        InitializeModules();
        InitializeLights();
        InitializeStorage();
        InitializeInterface();
        InitializeEquipmentModules();
        InitializeDeviceModules();
        InitializeMobilityModules();
        InitializeUtilityModules();
        InitializeArmorModules();
        InitializeEmpModules();
        InitializeDepartmentModules();
        InitializeHolsterModules();
        InitializeClamp();
        InitializeTransport();
        InitializeHat();
        InitializeAntagonistModules();
        InitializeArmorUtility();
        InitializeAshCoat();
        InitializeLoaderModules();
        InitializeEquipmentCopy();
        InitializeAssistants();
    }

    public EntityUid? GetCore(Entity<ModSuitComponent> suit) => _itemSlots.GetItemOrNull(suit.Owner, suit.Comp.CoreSlotId);

    public Entity<BatteryComponent>? GetBattery(Entity<ModSuitComponent> suit)
    {
        if (GetCore(suit) is not { } core || !HasComp<ModCoreComponent>(core))
            return null;

        return _powerCell.TryGetBatteryFromEntityOrSlot(core, out var battery) ? battery : null;
    }

    public bool HasPower(Entity<ModSuitComponent> suit) => suit.Comp.Powered;

    internal bool HasModuleCharge(Entity<ModSuitComponent> suit, float amount) =>
        GetBattery(suit) is { } battery && _battery.GetCharge(battery.Owner).Charge >= amount;

    internal bool TryUseModuleCharge(Entity<ModSuitComponent> suit, float amount) =>
        GetBattery(suit) is { } battery && _battery.TryUseCharge(battery.AsNullable(), amount);

    private void OnFindBattery(Entity<ModSuitComponent> suit, ref FindBatteryEvent args) =>
        args.FoundBattery ??= GetBattery(suit);

    private void OnFindWornBattery(Entity<ModSuitComponent> suit, ref InventoryRelayedEvent<FindBatteryEvent> args) =>
        OnFindBattery(suit, ref args.Args);

    private void OnSuitItemInserted(Entity<ModSuitComponent> suit, ref EntInsertedIntoContainerMessage args)
    {
        if (!_net.IsClient && !_timing.ApplyingState && IsAssistantSlot(args.Container.ID))
            RefreshAssistant(suit);

        if (args.Container.ID != suit.Comp.CoreSlotId || _timing.ApplyingState)
            return;

        UpdatePower(suit);
        RaiseBatteryChanged(suit);
        RefreshDrain(suit);
    }

    private void OnSuitItemRemoved(Entity<ModSuitComponent> suit, ref EntRemovedFromContainerMessage args)
    {
        if (!_net.IsClient && !_timing.ApplyingState && IsAssistantSlot(args.Container.ID))
            RefreshAssistant(suit);

        if (_net.IsClient || _timing.ApplyingState || TerminatingOrDeleted(suit))
            return;

        if (args.Container == suit.Comp.ModulesContainer && TryComp(args.Entity, out ModModuleComponent? module))
        {
            DetachModule(suit, (args.Entity, module));
            return;
        }

        if (args.Container.ID != suit.Comp.CoreSlotId)
            return;

        UpdatePower(suit);
        RaiseBatteryChanged(suit);
        if (!TerminatingOrDeleted(args.Entity)
            && _powerCell.TryGetBatteryFromEntityOrSlot(args.Entity, out var detached))
            _battery.RefreshChargeRate(detached.Value.AsNullable());
    }

    private void OnCoreBatteryStateChanged(Entity<ModCoreComponent> core, ref BatteryStateChangedEvent args)
    {
        if ((args.OldState == BatteryState.Empty || args.NewState == BatteryState.Empty)
            && TryGetSuit(core, out var suit))
            UpdatePower(suit);
    }

    private void OnCoreCellChanged(Entity<ModCoreComponent> core, ref PowerCellChangedEvent args)
    {
        if (!TryGetSuit(core, out var suit))
            return;

        UpdatePower(suit);
        RaiseBatteryChanged(suit);
    }

    private void RaiseBatteryChanged(Entity<ModSuitComponent> suit)
    {
        if (_net.IsClient || !HasComp<NinjaSuitComponent>(suit) || suit.Comp.Wearer is not { } wearer)
            return;

        var ev = new ModSuitBatteryChangedEvent(wearer, GetBattery(suit)?.Owner);
        RaiseLocalEvent(suit.Owner, ref ev);
    }

    private void OnNinjaCloakAttempt(Entity<ModSuitComponent> suit, ref ItemToggleActivateAttemptEvent args)
    {
        if (!HasComp<NinjaSuitComponent>(suit) || args.Cancelled)
            return;

        if (args.User != suit.Comp.Wearer || !HasModuleCharge(suit, NinjaCloakDrain))
        {
            args.Cancelled = true;
            args.Popup = "ninja-no-power";
        }
    }

    private void OnNinjaCloakToggled(Entity<ModSuitComponent> suit, ref ItemToggledEvent args)
    {
        if (HasComp<NinjaSuitComponent>(suit))
            RefreshDrain(suit);
    }

    private void OnCoreRefreshChargeRate(Entity<ModCoreComponent> core, ref RefreshChargeRateEvent args)
    {
        if (TryGetSuit(core, out var suit))
        {
            args.NewChargeRate -= GetTotalDrain(suit);
            if (HasComp<NinjaSuitComponent>(suit) && _itemToggle.IsActivated(suit.Owner))
                args.NewChargeRate -= NinjaCloakDrain;
        }
    }

    private const float NinjaCloakDrain = 3.6f;

    public float GetTotalDrain(Entity<ModSuitComponent> suit) => suit.Comp.State switch
    {
        ModSuitState.Inactive => 0f,
        ModSuitState.Active => _prototypes.Index(suit.Comp.Theme).ChargeDrain + GetModuleDrain(suit),
        _ => _prototypes.Index(suit.Comp.Theme).ChargeDrain,
    };

    private bool TryGetSuit(EntityUid core, out Entity<ModSuitComponent> suit)
    {
        suit = default;
        if (!_containers.TryGetContainingContainer(core, out var container)
            || !TryComp(container.Owner, out ModSuitComponent? control)
            || TerminatingOrDeleted(container.Owner)
            || container.ID != control.CoreSlotId)
            return false;

        suit = (container.Owner, control);
        return true;
    }

    private void RefreshDrain(Entity<ModSuitComponent> suit)
    {
        if (!TerminatingOrDeleted(suit) && GetBattery(suit) is { } battery)
            _battery.RefreshChargeRate(battery.AsNullable());
    }

    private void UpdatePower(Entity<ModSuitComponent> suit)
    {
        if (_net.IsClient || _timing.ApplyingState || TerminatingOrDeleted(suit))
            return;

        var hasPower = GetBattery(suit) is { } battery && _battery.GetCharge(battery.Owner).Charge > 0f;
        if (suit.Comp.Powered == hasPower)
            return;

        suit.Comp.Powered = hasPower;
        Dirty(suit);
        if (!hasPower && HasComp<NinjaSuitComponent>(suit))
            _itemToggle.TryDeactivate(suit.Owner, suit.Comp.Wearer, predicted: false, consciousAction: false);
        if (!hasPower && suit.Comp.State == ModSuitState.Activating)
            StopActivation(suit);
        else
            RefreshModuleAvailability(suit);
        RefreshWearerPower(suit);
        RefreshSuitAssistantActions(suit.Owner);
    }

    private void OnRefreshSuitSpeed(Entity<ModSuitComponent> suit, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (suit.Comp.State == ModSuitState.Active && !suit.Comp.Powered && !suit.Comp.Projected)
            args.Args.ModifySpeed(suit.Comp.UnpoweredSpeedModifier);

        ApplyModuleSpeedModifiers(suit, args.Args);
    }

    private void RefreshWearerPower(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.Wearer is not { } wearer || TerminatingOrDeleted(wearer))
            return;

        _movement.RefreshMovementSpeedModifiers(wearer);
        UpdatePowerAlert(suit);
    }

    private void UpdatePowerAlert(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.Wearer is not { } wearer || TerminatingOrDeleted(wearer))
            return;

        if (suit.Comp.State != ModSuitState.Active || GetBattery(suit) is not { } battery)
        {
            ClearPowerAlert(suit, wearer);
            return;
        }

        var severity = (short) Math.Clamp((int) (_battery.GetChargeLevel(battery.AsNullable()) * 5), 0, 5);
        if (suit.Comp.ShownPowerAlert != severity)
        {
            _alerts.ShowAlert(wearer, PowerAlert, severity);
            suit.Comp.ShownPowerAlert = severity;
        }
    }

    private void ClearPowerAlert(Entity<ModSuitComponent> suit, EntityUid wearer)
    {
        if (suit.Comp.ShownPowerAlert == null)
            return;

        _alerts.ClearAlert(wearer, PowerAlert);
        suit.Comp.ShownPowerAlert = null;
    }
}
