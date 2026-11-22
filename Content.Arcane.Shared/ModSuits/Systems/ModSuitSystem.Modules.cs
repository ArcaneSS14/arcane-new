using System.Linq;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Wires;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string ModulesContainerId = "mod-modules";

    public bool TryGetInstalledModuleSuit(EntityUid? uid, out Entity<ModSuitComponent> suit)
    {
        if (uid is { } id && TryComp(id, out ModModuleComponent? module)
            && module.Suit is { } control
            && TryComp(control, out ModSuitComponent? component)
            && component.ModulesContainer?.Contains(id) == true)
        {
            suit = (control, component);
            return true;
        }

        suit = default;
        return false;
    }

    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    private void InitializeModules()
    {
        SubscribeLocalEvent<ModSuitComponent, InteractUsingEvent>(OnModuleInteractUsing, before: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<ModModuleComponent, ModModuleActionEvent>(OnModuleAction);
        SubscribeLocalEvent<ModModuleComponent, ModNinjaNetActionEvent>(OnNinjaNetAction);
        SubscribeLocalEvent<ModModuleComponent, EntityTerminatingEvent>(OnModuleTerminating);
    }

    private void SpawnStartingModules(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.ModulesContainer is not { Count: 0 })
            return;

        var theme = _prototypes.Index(suit.Comp.Theme);
        foreach (var prototype in theme.InbuiltModules)
            SpawnStartingModule(suit, prototype, builtIn: true);
        foreach (var prototype in suit.Comp.InbuiltModules)
            SpawnStartingModule(suit, prototype, builtIn: true);
        foreach (var prototype in suit.Comp.StartingModules)
            SpawnStartingModule(suit, prototype, builtIn: false);
    }

    private void SpawnStartingModule(Entity<ModSuitComponent> suit, EntProtoId prototype, bool builtIn)
    {
        var uid = SpawnAttachedTo(prototype, Transform(suit.Owner).Coordinates);
        if (TryComp(uid, out ModModuleComponent? component))
        {
            var module = (uid, component);
            component.Removable &= !builtIn;
            if ((builtIn || GetInstallFailure(suit, module) == null) && InstallModule(suit, module))
                return;
        }

        Log.Warning($"Cannot install starting module {prototype} in {ToPrettyString(suit)}");
        QueueDel(uid);
    }

    public int GetComplexity(Entity<ModSuitComponent> suit)
    {
        var complexity = 0;
        if (suit.Comp.ModulesContainer is not { } modules)
            return complexity;

        foreach (var uid in modules.ContainedEntities)
        {
            if (TryComp(uid, out ModModuleComponent? module))
                complexity += module.Complexity;
        }

        return complexity;
    }

    private LocId? GetInstallFailure(Entity<ModSuitComponent> suit, Entity<ModModuleComponent> module)
    {
        var theme = _prototypes.Index(suit.Comp.Theme);
        if (_whitelist.IsWhitelistFail(theme.AllowedModules, module) || _whitelist.IsWhitelistPass(theme.BlacklistedModules, module))
            return "modsuit-module-not-allowed";

        if (GetComplexity(suit) + module.Comp.Complexity > theme.ComplexityMax)
            return "modsuit-module-too-complex";

        foreach (var slot in module.Comp.RequiredParts)
        {
            if (!suit.Comp.PartEntities.TryGetValue(slot, out var part) || TerminatingOrDeleted(part))
                return "modsuit-module-missing-part";
        }

        var prototype = MetaData(module.Owner).EntityPrototype;
        if (suit.Comp.ModulesContainer is not { } modules)
            return "modsuit-module-not-allowed";

        foreach (var uid in modules.ContainedEntities)
        {
            if (prototype != null && MetaData(uid).EntityPrototype == prototype)
                return "modsuit-module-duplicate";

            if (_whitelist.IsWhitelistPass(module.Comp.Incompatible, uid)
                || TryComp(uid, out ModModuleComponent? installed) && _whitelist.IsWhitelistPass(installed.Incompatible, module))
                return "modsuit-module-incompatible";
        }

        return null;
    }

    public bool TryInstallModule(Entity<ModSuitComponent> suit, EntityUid moduleUid, EntityUid user)
    {
        if (_net.IsClient || !TryComp(moduleUid, out ModModuleComponent? component) || component.Suit != null
            || !CanReachSuit(suit, user) || !CanAccessModules(suit, user) || !_actionBlocker.CanInteract(user, suit)
            || !_inventory.CanAccess(user, user, moduleUid))
            return false;

        var module = (moduleUid, component);
        var failure = GetMaintenanceFailure(suit) ?? GetInstallFailure(suit, module);
        if (failure is { } reason)
        {
            _popup.PopupEntity(Loc.GetString(reason), suit, user);
            return false;
        }

        return InstallModule(suit, module);
    }

    private bool InstallModule(Entity<ModSuitComponent> suit, Entity<ModModuleComponent> module)
    {
        if (suit.Comp.ModulesContainer is not { } modules || !_containers.Insert(module.Owner, modules))
            return false;

        module.Comp.Suit = suit;
        Dirty(module);
        var installed = new ModModuleInstalledEvent(suit);
        RaiseLocalEvent(module.Owner, ref installed);
        if (suit.Comp.Wearer is { } wearer)
        {
            OnModuleWorn(module, wearer);
        }
        RefreshModuleAvailability(suit);
        return true;
    }

    public bool TryRemoveModule(Entity<ModSuitComponent> suit, EntityUid moduleUid, EntityUid user)
    {
        if (_net.IsClient || suit.Comp.ModulesContainer is not { } modules || !modules.Contains(moduleUid)
            || !TryComp(moduleUid, out ModModuleComponent? module) || !CanReachSuit(suit, user)
            || !CanAccessModules(suit, user) || !_actionBlocker.CanInteract(user, suit))
            return false;

        var failure = GetMaintenanceFailure(suit);
        if (failure != null || !module.Removable)
        {
            _popup.PopupEntity(Loc.GetString(failure ?? "modsuit-module-not-removable"), suit, user);
            return false;
        }

        if (!_containers.Remove(moduleUid, modules, destination: Transform(user).Coordinates))
            return false;

        _hands.PickupOrDrop(user, moduleUid);
        return true;
    }

    private LocId? GetMaintenanceFailure(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.State != ModSuitState.Inactive)
            return "modsuit-deactivate-first";

        return HasComp<WiresPanelComponent>(suit) && _wires.IsPanelOpen(suit.Owner)
            ? null
            : "modsuit-open-panel-first";
    }

    private bool CanReachSuit(Entity<ModSuitComponent> suit, EntityUid user) =>
        !TerminatingOrDeleted(suit) && (IsAssistant(suit, user) || (suit.Comp.Wearer is { } wearer
            ? wearer == user
            : HasComp<HandsComponent>(user) && _interaction.InRangeAndAccessible(user, suit.Owner)));

    private bool CanAccessModules(Entity<ModSuitComponent> suit, EntityUid user)
    {
        if (suit.Comp.Projected || suit.Comp.ModulesContainer is not { } modules)
            return false;

        var attempt = new ModSuitAccessAttemptEvent(user);
        foreach (var uid in modules.ContainedEntities)
        {
            RaiseLocalEvent(uid, ref attempt);
            if (attempt.Cancelled)
                return false;
        }

        return true;
    }

    private bool TryUseModule(Entity<ModModuleComponent> module, EntityUid user, BaseActionEvent? action = null)
    {
        var component = module.Comp;
        if (_net.IsClient || component.Type == ModModuleType.Passive || component.Suit is not { } uid
            || !TryComp(uid, out ModSuitComponent? control) || control.ModulesContainer?.Contains(module.Owner) != true)
            return false;

        var suit = (uid, control);
        if (TryComp(user, out ModSphereFormComponent? sphere) && sphere.Module != module.Owner)
            return false;

        if (!CanReachSuit(suit, user) || !CanAccessModules(suit, user)
            || (!component.AllowFlags.HasFlag(ModModuleAllowFlags.Unworn) && !IsSuitOperator(suit, user))
            || (!component.AllowFlags.HasFlag(ModModuleAllowFlags.Incapacitated) && !_actionBlocker.CanInteract(user, uid)))
            return false;

        var actor = user;
        if (IsAssistant(suit, user) && control.Wearer is { } wearer)
            user = wearer;

        if (component.Active && component.Type is ModModuleType.Toggle or ModModuleType.Active)
        {
            SetModuleActive(suit, module, false);
            return true;
        }

        if (!IsModuleAvailable(suit, module) || _timing.CurTime < component.NextUse)
            return false;

        var battery = GetBattery(suit);
        if (component.UsePower > 0f && (battery == null || _battery.GetCharge(battery.Value.Owner).Charge < component.UsePower))
            return false;

        if (component.Type == ModModuleType.Usable)
        {
            var used = new ModModuleUsedEvent(uid, user, action, actor);
            RaiseLocalEvent(module.Owner, ref used);
            if (!used.Handled)
                return false;
        }
        else
        {
            var attempt = new ModModuleActivateAttemptEvent(uid, user);
            RaiseLocalEvent(module.Owner, ref attempt);
            if (attempt.Cancelled)
                return false;

            if (component.Type == ModModuleType.Active && control.ModulesContainer is { } modules)
            {
                foreach (var other in modules.ContainedEntities.ToArray())
                {
                    if (other != module.Owner && TryComp(other, out ModModuleComponent? selected) && selected.Type == ModModuleType.Active)
                        SetModuleActive(suit, (other, selected), false);
                }
            }

            SetModuleActive(suit, module, true);
        }

        if (battery is { } usedBattery && component.UsePower > 0f)
            _battery.TryUseCharge(usedBattery.AsNullable(), component.UsePower);
        component.NextUse = _timing.CurTime + component.Cooldown;
        Dirty(module);
        _actions.SetCooldown(component.ActionEntity, component.Cooldown);
        if (action != null && action.Action.Owner.IsValid() && action.Action.Owner != component.ActionEntity)
            _actions.SetCooldown(action.Action.Owner, component.Cooldown);
        return true;
    }

    private bool IsModuleAvailable(Entity<ModSuitComponent> suit, Entity<ModModuleComponent> module)
    {
        var worksInactive = module.Comp.AllowFlags.HasFlag(ModModuleAllowFlags.Inactive);
        if (suit.Comp.State != ModSuitState.Active && !worksInactive)
            return false;

        return suit.Comp.Powered || worksInactive && module.Comp.IdlePower <= 0f
            && module.Comp.ActivePower <= 0f && module.Comp.UsePower <= 0f;
    }

    private void RefreshModuleAvailability(Entity<ModSuitComponent> suit)
    {
        if (_net.IsClient || suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var uid in modules.ContainedEntities.ToArray())
        {
            if (!TryComp(uid, out ModModuleComponent? component) || component.Suit != suit.Owner)
                continue;

            var module = (uid, component);
            var available = IsModuleAvailable(suit, module);
            if (!available || component.Type == ModModuleType.Passive)
                SetModuleActive(suit, module, available);
        }

        RefreshDrain(suit);
    }

    private float GetModuleDrain(Entity<ModSuitComponent> suit)
    {
        var drain = 0f;
        if (suit.Comp.ModulesContainer is not { } modules)
            return drain;

        foreach (var uid in modules.ContainedEntities)
        {
            if (!TryComp(uid, out ModModuleComponent? module) || module.Suit != suit.Owner)
                continue;

            drain += module.IdlePower + (module.Active ? module.ActivePower : 0f);
        }

        return drain;
    }

    private void SetModuleActive(Entity<ModSuitComponent> suit, Entity<ModModuleComponent> module, bool active)
    {
        if (module.Comp.Active == active)
            return;

        module.Comp.Active = active;
        Dirty(module);
        _actions.SetToggled(module.Comp.ActionEntity, active);
        if (suit.Comp.Assistant is { } assistant && TryComp(assistant, out ModAssistantComponent? helper)
            && helper.ModuleActions.TryGetValue(module.Owner, out var helperAction))
            _actions.SetToggled(helperAction, active);
        if (active)
        {
            var activated = new ModModuleActivatedEvent(suit, suit.Comp.Wearer);
            RaiseLocalEvent(module.Owner, ref activated);
        }
        else
        {
            var deactivated = new ModModuleDeactivatedEvent(suit, suit.Comp.Wearer);
            RaiseLocalEvent(module.Owner, ref deactivated);
        }

        RefreshDrain(suit);
    }

    private void UpdateModuleWearer(Entity<ModSuitComponent> suit, EntityUid wearer, bool equipped)
    {
        if (suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var uid in modules.ContainedEntities.ToArray())
        {
            if (!TryComp(uid, out ModModuleComponent? module))
                continue;

            if (equipped)
                OnModuleWorn((uid, module), wearer);
            else
            {
                if (!module.AllowFlags.HasFlag(ModModuleAllowFlags.Unworn) && module.Type != ModModuleType.Passive)
                    SetModuleActive(suit, (uid, module), false);
                _actions.RemoveProvidedActions(wearer, uid);
                CloseDeviceSelector(uid, wearer);
            }
        }
    }

    private void OnModuleWorn(Entity<ModModuleComponent> module, EntityUid wearer)
    {
        GrantModuleAction(module, wearer);
        GrantDeviceSelector(module, wearer);
        var worn = new ModModuleWornEvent(wearer);
        RaiseLocalEvent(module.Owner, ref worn);
    }

    private void GrantModuleAction(Entity<ModModuleComponent> module, EntityUid wearer)
    {
        if (module.Comp.Pinned && module.Comp.Type != ModModuleType.Passive && module.Comp.Action is { } proto
            && _actions.AddAction(wearer, ref module.Comp.ActionEntity, proto, module))
        {
            var action = module.Comp.ActionEntity.Value;
            _metadata.SetEntityName(action, Name(module));
            _metadata.SetEntityDescription(action, Description(module));
            _actions.SetEntityIcon(action, module);
            _actions.SetToggled(module.Comp.ActionEntity, module.Comp.Active);
        }
    }

    private void DetachModule(Entity<ModSuitComponent> suit, Entity<ModModuleComponent> module)
    {
        if (module.Comp.Suit != suit.Owner)
            return;

        module.Comp.Suit = null;
        SetModuleActive(suit, module, false);
        if (suit.Comp.Wearer is { } wearer)
        {
            _actions.RemoveProvidedActions(wearer, module);
            CloseDeviceSelector(module, wearer);
        }
        Dirty(module);
        var removed = new ModModuleRemovedEvent(suit);
        RaiseLocalEvent(module.Owner, ref removed);
        RefreshDrain(suit);
    }

    private void OnModuleInteractUsing(Entity<ModSuitComponent> suit, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (HasComp<ModModuleComponent>(args.Used))
        {
            TryInstallModule(suit, args.Used, args.User);
            args.Handled = true;
            return;
        }

        if (!CanAccessModules(suit, args.User))
            return;

        if (TryUseAssistantCard(suit, args.User, args.Used))
        {
            args.Handled = true;
            return;
        }

        var use = new ModSuitUseItemEvent(args.User, args.Used);
        RaiseLocalEvent(suit.Owner, ref use);
        args.Handled = use.Handled;
    }

    private void OnModuleAction(Entity<ModModuleComponent> module, ref ModModuleActionEvent args)
    {
        if (!args.Handled && TryUseModule(module, args.Performer, args))
            args.Handled = true;
    }

    private void OnNinjaNetAction(Entity<ModModuleComponent> module, ref ModNinjaNetActionEvent args)
    {
        if (!args.Handled && TryUseModule(module, args.Performer, args))
            args.Handled = true;
    }

    private void OnModuleTerminating(Entity<ModModuleComponent> module, ref EntityTerminatingEvent args)
    {
        if (!_net.IsClient && module.Comp.Suit is { } uid && !TerminatingOrDeleted(uid) && TryComp(uid, out ModSuitComponent? suit))
            DetachModule((uid, suit), module);
    }
}
