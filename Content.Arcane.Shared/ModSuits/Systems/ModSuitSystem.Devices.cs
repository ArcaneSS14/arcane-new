using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Hands;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string DeviceSlotId = "mod-device";
    private readonly HashSet<EntityUid> _pendingDeviceReturn = [];

    private void InitializeDeviceModules()
    {
        SubscribeLocalEvent<ModDeviceModuleComponent, ModModuleActivateAttemptEvent>(OnDeviceActivationAttempt);
        SubscribeLocalEvent<ModDeviceModuleComponent, ModModuleActivatedEvent>(OnDeviceActivated);
        SubscribeLocalEvent<ModDeviceModuleComponent, ModModuleDeactivatedEvent>(OnDeviceDeactivated);
        SubscribeLocalEvent<ModDeviceModuleComponent, EntityTerminatingEvent>(OnDeviceModuleTerminating);
        SubscribeLocalEvent<ModDeviceItemComponent, ContainerGettingInsertedAttemptEvent>(OnDeviceInsertionAttempt);
        SubscribeLocalEvent<ModDeviceItemComponent, GotUnequippedHandEvent>(OnDeviceReleased);
        SubscribeLocalEvent<ModDeviceItemComponent, EntityTerminatingEvent>(OnDeviceTerminating);
    }

    private void OnDeviceActivationAttempt(Entity<ModDeviceModuleComponent> module, ref ModModuleActivateAttemptEvent args)
    {
        if (!args.Cancelled && _hands.CountFreeHands(args.User) == 0)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-module-hands-full"), args.Suit, args.User);
            args.Cancelled = true;
        }
    }

    private void OnDeviceActivated(Entity<ModDeviceModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is { } wearer && GetOrSpawnDevice(module) is { } device
            && _hands.TryPickupAnyHand(wearer, device, checkActionBlocker: false))
            return;

        DeactivateDeviceModule(module);
    }

    private EntityUid? GetOrSpawnDevice(Entity<ModDeviceModuleComponent> module)
    {
        if (module.Comp.DeviceEntity is { } existing && !TerminatingOrDeleted(existing))
            return existing;

        if (!TrySpawnInContainer(module.Comp.Device, module, DeviceSlotId, out var device))
            return null;

        EnsureComp<ModDeviceItemComponent>(device.Value).Module = module;
        module.Comp.DeviceEntity = device;
        return device;
    }

    private void OnDeviceDeactivated(Entity<ModDeviceModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (module.Comp.DeviceEntity is { } uid && !_pendingDeviceReturn.Contains(uid))
            ReturnDevice(module, uid);
    }

    private void ReturnDevice(Entity<ModDeviceModuleComponent> module, EntityUid device)
    {
        if (TerminatingOrDeleted(device))
            return;

        if (TerminatingOrDeleted(module) || !_containers.TryGetContainer(module, DeviceSlotId, out var storage)
            || (!storage.Contains(device) && !_containers.Insert(device, storage)))
            QueueDel(device);
    }

    private void OnDeviceInsertionAttempt(Entity<ModDeviceItemComponent> item, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Container.Owner == item.Comp.Module && args.Container.ID == DeviceSlotId)
            return;

        if (!TryComp(item.Comp.Module, out ModModuleComponent? module) || !module.Active
            || !TryComp(module.Suit, out ModSuitComponent? suit) || suit.Wearer != args.Container.Owner
            || !_hands.TryGetHand(args.Container.Owner, args.Container.ID, out _))
            args.Cancel();
    }

    private void OnDeviceReleased(Entity<ModDeviceItemComponent> item, ref GotUnequippedHandEvent args)
    {
        if (_net.IsClient || _timing.ApplyingState || TerminatingOrDeleted(item)
            || !TryComp(item.Comp.Module, out ModDeviceModuleComponent? module)
            || !TryComp(item.Comp.Module, out ModModuleComponent? installed) || !installed.Active
            || !_pendingDeviceReturn.Add(item))
            return;

        var uid = item.Comp.Module;
        DeactivateDeviceModule((uid, module));
        Timer.Spawn(0, () =>
        {
            _pendingDeviceReturn.Remove(item);
            if (module.DeviceEntity == item.Owner && !installed.Active)
                ReturnDevice((uid, module), item);
        });
    }

    private void OnDeviceModuleTerminating(Entity<ModDeviceModuleComponent> module, ref EntityTerminatingEvent args)
    {
        if (module.Comp.DeviceEntity is { } device && !TerminatingOrDeleted(device))
            QueueDel(device);
    }

    private void OnDeviceTerminating(Entity<ModDeviceItemComponent> item, ref EntityTerminatingEvent args)
    {
        var uid = item.Comp.Module;
        if (TerminatingOrDeleted(uid) || !TryComp(uid, out ModDeviceModuleComponent? module) || module.DeviceEntity != item.Owner)
            return;

        module.DeviceEntity = null;
        DeactivateDeviceModule((uid, module));
    }

    private void DeactivateDeviceModule(Entity<ModDeviceModuleComponent> device)
    {
        if (TryComp(device, out ModModuleComponent? module) && module.Active && module.Suit is { } uid
            && TryComp(uid, out ModSuitComponent? suit))
            SetModuleActive((uid, suit), (device.Owner, module), false);
    }
}
