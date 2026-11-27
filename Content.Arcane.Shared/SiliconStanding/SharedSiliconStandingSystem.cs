using Content.Arcane.Common.SiliconStanding;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.SiliconStanding;

public sealed partial class SharedSiliconStandingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SiliconStandingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconStandingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SiliconStandingComponent, BorgSubtypeSelectedEvent>(OnSubtypeSelected);
        SubscribeLocalEvent<SiliconStandingComponent, ToggleSiliconRestingEvent>(OnToggle);
        SubscribeLocalEvent<SiliconStandingComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestingChanged);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentRemove>(OnRestingRemoved);
    }

    private void OnMapInit(Entity<SiliconStandingComponent> ent, ref MapInitEvent args) => UpdateAction(ent);
    private void OnSubtypeSelected(Entity<SiliconStandingComponent> ent, ref BorgSubtypeSelectedEvent args) => UpdateAction(ent);

    private void OnShutdown(Entity<SiliconStandingComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            _actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }

    private void UpdateAction(Entity<SiliconStandingComponent> ent)
    {
        if (!_net.IsServer)
            return;

        if (CanRest(ent))
            _actions.AddAction(ent.Owner, ref ent.Comp.Action, SiliconStandingComponent.ToggleAction);
        else
        {
            if (HasComp<SiliconRestingComponent>(ent))
                SetResting(ent, false);
            _actions.RemoveAction(ent.Owner, ent.Comp.Action);
            ent.Comp.Action = null;
        }
        Dirty(ent);
    }

    private void OnToggle(Entity<SiliconStandingComponent> ent, ref ToggleSiliconRestingEvent args)
    {
        if (args.Handled || !CanRest(ent))
            return;

        SetResting(ent, !HasComp<SiliconRestingComponent>(ent));
        args.Handled = true;
    }

    private bool CanRest(EntityUid uid)
    {
        return HasComp<BorgChassisComponent>(uid)
               && TryComp<BorgSwitchableSubtypeComponent>(uid, out var subtype)
               && _prototypes.TryIndex(subtype.BorgSubtype, out var proto)
               && proto.Visuals.RestBodyState != null;
    }

    public void SetResting(EntityUid uid, bool resting)
    {
        if (resting)
            EnsureComp<SiliconRestingComponent>(uid);
        else
            RemComp<SiliconRestingComponent>(uid);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, resting, appearance);
        _actionBlocker.UpdateCanMove(uid);
    }

    private void OnCanMove(Entity<SiliconStandingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (HasComp<SiliconRestingComponent>(ent))
            args.Cancel();
    }

    private void OnRestingChanged(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        _actionBlocker.UpdateCanMove(ent);
        var ev = new BorgRestVisualsChangedEvent(true);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void OnRestingRemoved(Entity<SiliconRestingComponent> ent, ref ComponentRemove args)
    {
        _actionBlocker.UpdateCanMove(ent);
        var ev = new BorgRestVisualsChangedEvent(false);
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}
