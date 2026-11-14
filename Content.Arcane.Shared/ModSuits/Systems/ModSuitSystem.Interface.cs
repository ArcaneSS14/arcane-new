using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Verbs;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private void InitializeInterface()
    {
        SubscribeLocalEvent<ModSuitComponent, GetVerbsEvent<ActivationVerb>>(OnSuitInterfaceVerb);
        SubscribeLocalEvent<ModSuitComponent, BoundUserInterfaceMessageAttempt>(OnSuitMessageAttempt);
        SubscribeLocalEvent<ModSuitComponent, ModSuitActivateMessage>(OnInterfaceActivate);
        SubscribeLocalEvent<ModSuitComponent, ModSuitToggleAllPartsMessage>(OnInterfaceToggleAll);
        SubscribeLocalEvent<ModSuitComponent, ModSuitTogglePartMessage>(OnInterfaceTogglePart);
        SubscribeLocalEvent<ModSuitComponent, ModSuitModuleToggleMessage>(OnInterfaceUseModule);
        SubscribeLocalEvent<ModSuitComponent, ModSuitModulePinMessage>(OnInterfacePinModule);
        SubscribeLocalEvent<ModSuitComponent, ModSuitModuleRemoveMessage>(OnInterfaceRemoveModule);
    }

    private void OnSuitInterfaceVerb(Entity<ModSuitComponent> suit, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (_net.IsClient || !args.CanAccess || !args.CanInteract || !_ui.HasUi(suit, ModSuitUiKey.Key)
            || !CanReachSuit(suit, args.User) || !CanAccessModules(suit, args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("modsuit-ui-title"),
            Act = () =>
            {
                if (CanReachSuit(suit, user) && CanAccessModules(suit, user) && _actionBlocker.CanInteract(user, suit))
                    _ui.TryOpenUi(suit.Owner, ModSuitUiKey.Key, user);
            },
        });
    }

    private void OnSuitMessageAttempt(Entity<ModSuitComponent> suit, ref BoundUserInterfaceMessageAttempt args)
    {
        if (_net.IsClient || args.UiKey is not ModSuitUiKey.Key)
            return;

        if (!CanReachSuit(suit, args.Actor) || !CanAccessModules(suit, args.Actor))
            args.Cancel();
    }

    private void OnInterfaceActivate(Entity<ModSuitComponent> suit, ref ModSuitActivateMessage args) =>
        TryToggleActivation(suit, args.Actor);

    private void OnInterfaceToggleAll(Entity<ModSuitComponent> suit, ref ModSuitToggleAllPartsMessage args) =>
        TryToggleAllParts(suit, args.Actor);

    private void OnInterfaceTogglePart(Entity<ModSuitComponent> suit, ref ModSuitTogglePartMessage args)
    {
        if (_net.IsClient || suit.Comp.Deploying || suit.Comp.Wearer != args.Actor || !CanAccessModules(suit, args.Actor))
            return;

        if (IsPartDeployed(suit, args.Slot))
            TryRetractPart(suit, args.Slot, args.Actor);
        else
            TryDeployPart(suit, args.Slot, args.Actor);
    }

    private void OnInterfaceUseModule(Entity<ModSuitComponent> suit, ref ModSuitModuleToggleMessage args)
    {
        if (GetInstalledModule(suit, args.Module) is { } module)
            TryUseModule(module, args.Actor);
    }

    private void OnInterfacePinModule(Entity<ModSuitComponent> suit, ref ModSuitModulePinMessage args)
    {
        if (_net.IsClient || !CanReachSuit(suit, args.Actor) || !CanAccessModules(suit, args.Actor)
            || !_actionBlocker.CanInteract(args.Actor, suit) || GetInstalledModule(suit, args.Module) is not { } module
            || module.Comp.Type == ModModuleType.Passive || module.Comp.Action == null)
            return;

        module.Comp.Pinned = !module.Comp.Pinned;
        Dirty(module);
        if (suit.Comp.Wearer is not { } wearer)
            return;

        if (module.Comp.Pinned)
            GrantModuleAction(module, wearer);
        else
        {
            if (module.Comp.ActionEntity is { } action)
                _actions.RemoveProvidedAction(wearer, module, action);
        }
    }

    private void OnInterfaceRemoveModule(Entity<ModSuitComponent> suit, ref ModSuitModuleRemoveMessage args)
    {
        if (GetInstalledModule(suit, args.Module) is { } module)
            TryRemoveModule(suit, module, args.Actor);
    }

    private Entity<ModModuleComponent>? GetInstalledModule(Entity<ModSuitComponent> suit, NetEntity netModule)
    {
        if (!TryGetEntity(netModule, out var uid) || suit.Comp.ModulesContainer?.Contains(uid.Value) != true
            || !TryComp(uid, out ModModuleComponent? module) || module.Suit != suit.Owner)
            return null;

        return (uid.Value, module);
    }
}
