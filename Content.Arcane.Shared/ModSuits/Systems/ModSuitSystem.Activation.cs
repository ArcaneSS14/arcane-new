using Content.Arcane.Shared.ModSuits.Components;
using Content.Goobstation.Shared.Clothing;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Wires;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedWiresSystem _wires = default!;
    [Dependency] private ComponentTogglerSystem _componentToggler = default!;

    private void InitializeActivation()
    {
        SubscribeLocalEvent<ModSuitComponent, GetItemActionsEvent>(OnSuitActions);
        SubscribeLocalEvent<ModSuitComponent, ModSuitActivateActionEvent>(OnActivateAction);
        SubscribeLocalEvent<ModSuitComponent, ModSuitDeployActionEvent>(OnDeployAction);
        SubscribeLocalEvent<ModSuitComponent, AttemptChangePanelEvent>(OnPanelAttempt);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        UpdateThermalRegulators(frameTime);
        UpdateModJetpacks();
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ModSuitComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var suit = (uid, component);
            if (component.DeploymentSteps.Count > 0 && component.NextDeploymentStep <= now)
                UpdateDeployment(suit, now);

            if (component.State == ModSuitState.Active && component.NextPowerAlertUpdate <= now)
            {
                UpdatePowerAlert(suit);
                component.NextPowerAlertUpdate = now + TimeSpan.FromSeconds(1);
            }

            if (component.State is not (ModSuitState.Activating or ModSuitState.Deactivating)
                || component.NextActivationStep > now)
                continue;

            var sealing = component.State == ModSuitState.Activating;
            if (sealing && (!component.Powered || !AllPartsDeployed(suit)))
            {
                StopActivation(suit);
                continue;
            }

            if (!component.ActivationSteps.TryDequeue(out var slot))
            {
                if (sealing)
                    SetState(suit, ModSuitState.Active);
                else
                    StopActivation(suit);
                continue;
            }

            if (component.PartEntities.TryGetValue(slot, out var partUid) && TryComp(partUid, out ModPartComponent? part))
                SetPartSealed((partUid, part), sealing);

            component.NextActivationStep = now + _prototypes.Index(component.Theme).ActivationStepTime;
        }
    }

    public bool TryToggleActivation(Entity<ModSuitComponent> suit, EntityUid user)
    {
        if (_net.IsClient || suit.Comp.Wearer != user || suit.Comp.DeploymentSteps.Count > 0
            || !_actionBlocker.CanInteract(user, suit) || !CanAccessModules(suit, user))
            return false;

        switch (suit.Comp.State)
        {
            case ModSuitState.Inactive:
                if (!suit.Comp.Powered || !AllPartsDeployed(suit)
                    || (HasComp<WiresPanelComponent>(suit) && _wires.IsPanelOpen(suit.Owner)))
                    return false;

                if (suit.Comp.ActivationCharge > 0f && (GetBattery(suit) is not { } battery
                    || _battery.GetCharge(battery.AsNullable()) <= suit.Comp.ActivationCharge
                    || !_battery.TryUseCharge(battery.AsNullable(), suit.Comp.ActivationCharge)))
                    return false;

                foreach (var slot in suit.Comp.Parts.Keys)
                    suit.Comp.ActivationSteps.Enqueue(slot);
                SetState(suit, ModSuitState.Activating);
                break;
            case ModSuitState.Active:
                var slots = new List<string>(suit.Comp.Parts.Keys);
                slots.Reverse();
                foreach (var slot in slots)
                    suit.Comp.ActivationSteps.Enqueue(slot);
                SetState(suit, ModSuitState.Deactivating);
                break;
            default:
                return false;
        }

        suit.Comp.NextActivationStep = _timing.CurTime + _prototypes.Index(suit.Comp.Theme).ActivationStepTime;
        return true;
    }

    private bool AllPartsDeployed(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.Parts.Count == 0)
            return false;

        foreach (var slot in suit.Comp.Parts.Keys)
        {
            if (!IsPartDeployed(suit, slot))
                return false;
        }

        return true;
    }

    private void StopActivation(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.State == ModSuitState.Inactive || TerminatingOrDeleted(suit))
            return;

        suit.Comp.ActivationSteps.Clear();
        foreach (var uid in suit.Comp.PartEntities.Values)
        {
            if (!TerminatingOrDeleted(uid) && TryComp(uid, out ModPartComponent? part))
                SetPartSealed((uid, part), false);
        }

        SetState(suit, ModSuitState.Inactive);
    }

    private void SetPartSealed(Entity<ModPartComponent> part, bool sealedState)
    {
        if (part.Comp.Sealed == sealedState)
            return;

        if (sealedState && part.Comp.SealedComponents is { } registry)
        {
            foreach (var (name, entry) in registry)
            {
                if (!HasComp(part.Owner, entry.Component.GetType()))
                    part.Comp.AddedWhileSealed[name] = entry;
            }

            EntityManager.AddComponents(part, part.Comp.AddedWhileSealed);
        }
        else if (!sealedState && part.Comp.AddedWhileSealed.Count > 0)
        {
            EntityManager.RemoveComponents(part, part.Comp.AddedWhileSealed);
            part.Comp.AddedWhileSealed.Clear();
        }

        part.Comp.Sealed = sealedState;
        if (!HasComp<ItemToggleComponent>(part))
            _componentToggler.ToggleComponent(part, sealedState);
        Dirty(part);
        _appearance.SetData(part, SealableClothingVisuals.Sealed, sealedState);
    }

    private void SetState(Entity<ModSuitComponent> suit, ModSuitState state)
    {
        suit.Comp.State = state;
        var sealedState = state == ModSuitState.Active;
        Dirty(suit);
        _appearance.SetData(suit, SealableClothingVisuals.Sealed, sealedState);
        _actions.SetToggled(suit.Comp.ActivateActionEntity, state is ModSuitState.Active or ModSuitState.Activating);
        RefreshModuleAvailability(suit);
        RefreshWearerPower(suit);
    }

    private void OnSuitActions(Entity<ModSuitComponent> suit, ref GetItemActionsEvent args)
    {
        if (_net.IsClient || args.SlotFlags is not { } slot || (slot & SlotFlags.BACK) == 0)
            return;

        if (args.IsEquipping)
        {
            args.AddAction(ref suit.Comp.ActivateActionEntity, suit.Comp.ActivateAction);
            args.AddAction(ref suit.Comp.DeployActionEntity, suit.Comp.DeployAction);
        }
        else
        {
            args.AddAction(suit.Comp.ActivateActionEntity);
            args.AddAction(suit.Comp.DeployActionEntity);
        }
    }

    private void OnActivateAction(Entity<ModSuitComponent> suit, ref ModSuitActivateActionEvent args)
    {
        if (!args.Handled)
            args.Handled = TryToggleActivation(suit, args.Performer);
    }

    private void OnDeployAction(Entity<ModSuitComponent> suit, ref ModSuitDeployActionEvent args)
    {
        if (!args.Handled && TryToggleAllParts(suit, args.Performer))
            args.Handled = true;
    }

    public bool TryToggleAllParts(Entity<ModSuitComponent> suit, EntityUid user)
    {
        if (_net.IsClient || suit.Comp.Wearer != user || suit.Comp.State != ModSuitState.Inactive
            || suit.Comp.DeploymentSteps.Count > 0 || !_actionBlocker.CanInteract(user, suit) || !CanAccessModules(suit, user))
            return false;

        var retract = AllPartsDeployed(suit);
        var slots = new List<string>(suit.Comp.Parts.Keys);
        if (retract)
            slots.Reverse();

        foreach (var slot in slots)
        {
            if (IsPartDeployed(suit, slot) == retract)
                suit.Comp.DeploymentSteps.Enqueue(slot);
        }

        suit.Comp.Retracting = retract;
        suit.Comp.NextDeploymentStep = _timing.CurTime;
        suit.Comp.Deploying = suit.Comp.DeploymentSteps.Count > 0;
        Dirty(suit);
        return suit.Comp.Deploying;
    }

    private void UpdateDeployment(Entity<ModSuitComponent> suit, TimeSpan now)
    {
        if (suit.Comp.Wearer is not { } user || !CanAccessModules(suit, user))
        {
            CancelDeployment(suit);
            return;
        }

        var slot = suit.Comp.DeploymentSteps.Dequeue();
        var success = suit.Comp.Retracting ? TryRetractPart(suit, slot, user) : TryDeployPart(suit, slot, user);
        if (!success)
        {
            CancelDeployment(suit);
            _popup.PopupEntity(Loc.GetString("modsuit-deploy-failed"), suit, user);
        }
        else if (suit.Comp.DeploymentSteps.Count == 0)
            CancelDeployment(suit);

        suit.Comp.NextDeploymentStep = now + suit.Comp.DeployStepTime;
    }

    private void CancelDeployment(Entity<ModSuitComponent> suit)
    {
        if (!suit.Comp.Deploying)
            return;

        suit.Comp.DeploymentSteps.Clear();
        suit.Comp.Deploying = false;
        Dirty(suit);
    }

    private void OnPanelAttempt(Entity<ModSuitComponent> suit, ref AttemptChangePanelEvent args)
    {
        if (!args.Open && suit.Comp.State != ModSuitState.Inactive)
            args.Cancelled = true;
    }
}
