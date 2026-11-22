using System.Linq;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private void InitializeStorage()
    {
        SubscribeLocalEvent<ModSuitComponent, GetVerbsEvent<AlternativeVerb>>(OnStorageVerb);
        SubscribeLocalEvent<ModStorageModuleComponent, StorageInteractAttemptEvent>(OnModuleStorageAttempt);
        SubscribeLocalEvent<ModStorageModuleComponent, BoundUserInterfaceMessageAttempt>(OnModuleStorageUiAttempt);
        SubscribeLocalEvent<ModStorageModuleComponent, ModModuleDeactivatedEvent>(OnStorageDeactivated);
        SubscribeLocalEvent<ModStorageModuleComponent, ModModuleRemovedEvent>(OnStorageRemoved);
        SubscribeLocalEvent<ModStorageModuleComponent, EntityTerminatingEvent>(OnStorageModuleTerminating);
        SubscribeLocalEvent<ModStorageModuleComponent, ContainerIsInsertingAttemptEvent>(OnStorageItemAttempt);
        SubscribeLocalEvent<ModOreBagModuleComponent, ModModuleActivatedEvent>(OnOreBagActivated);
        SubscribeLocalEvent<ModOreBagModuleComponent, ModModuleDeactivatedEvent>(OnOreBagDeactivated);
    }

    private void OnStorageVerb(Entity<ModSuitComponent> suit, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (_net.IsClient || !args.CanInteract || !args.CanAccess || suit.Comp.ModulesContainer is not { } modules
            || !CanReachSuit(suit, args.User) || !CanAccessModules(suit, args.User))
            return;

        var user = args.User;
        foreach (var uid in modules.ContainedEntities)
        {
            if (!HasComp<ModStorageModuleComponent>(uid) || !IsModuleStorageActive(uid))
                continue;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("modsuit-open-module-storage", ("module", uid)),
                Act = () => OpenModuleStorage(suit, uid, user),
            });
        }
    }

    private void OpenModuleStorage(Entity<ModSuitComponent> suit, EntityUid module, EntityUid user)
    {
        if (IsModuleStorageActive(module) && suit.Comp.ModulesContainer?.Contains(module) == true
            && CanReachSuit(suit, user) && CanAccessModules(suit, user) && _actionBlocker.CanInteract(user, suit))
            _storage.OpenStorageUI(module, user);
    }

    private bool IsModuleStorageActive(EntityUid module) =>
        TryComp(module, out ModModuleComponent? component) && component.Active && component.Suit is { } suit
        && TryComp(suit, out ModSuitComponent? control) && control.ModulesContainer?.Contains(module) == true;

    private void OnModuleStorageAttempt(Entity<ModStorageModuleComponent> module, ref StorageInteractAttemptEvent args)
    {
        if (!_net.IsClient && !IsModuleStorageActive(module))
            args.Cancelled = true;
    }

    private void OnModuleStorageUiAttempt(Entity<ModStorageModuleComponent> module, ref BoundUserInterfaceMessageAttempt args)
    {
        if (_net.IsClient || args.UiKey is not StorageComponent.StorageUiKey.Key)
            return;

        if (!IsModuleStorageActive(module) || !TryComp(module, out ModModuleComponent? component)
            || component.Suit is not { } uid || !TryComp(uid, out ModSuitComponent? control)
            || !CanReachSuit((uid, control), args.Actor) || !CanAccessModules((uid, control), args.Actor))
            args.Cancel();
    }

    private void OnStorageItemAttempt(Entity<ModStorageModuleComponent> module, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID == StorageComponent.ContainerId && !_net.IsClient && !IsModuleStorageActive(module))
            args.Cancel();
    }

    private void OnStorageDeactivated(Entity<ModStorageModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (!_net.IsClient)
            _ui.CloseUi(module.Owner, StorageComponent.StorageUiKey.Key);
    }

    private void OnOreBagActivated(Entity<ModOreBagModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        var magnet = EnsureComp<MagnetPickupComponent>(module);
        magnet.NextScan = _timing.CurTime;
        Dirty(module, magnet);
    }

    private void OnOreBagDeactivated(Entity<ModOreBagModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        RemComp<MagnetPickupComponent>(module);
    }

    private void OnStorageRemoved(Entity<ModStorageModuleComponent> module, ref ModModuleRemovedEvent args) =>
        EmptyModuleStorage(module);

    private void OnStorageModuleTerminating(Entity<ModStorageModuleComponent> module, ref EntityTerminatingEvent args) =>
        EmptyModuleStorage(module);

    private void EmptyModuleStorage(Entity<ModStorageModuleComponent> module)
    {
        if (_net.IsClient || !TryComp(module, out StorageComponent? storage) || storage.Container.Count == 0)
            return;

        var coordinates = _transform.GetMoverCoordinates(module);
        foreach (var item in storage.Container.ContainedEntities.ToArray())
        {
            if (!TerminatingOrDeleted(item))
                _containers.Remove(item, storage.Container, force: true, destination: coordinates);
        }
    }
}
