using Content.Shared.Atmos.Rotting;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Hands; // Goobstation

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed partial class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [ViewVariables]
    public HashSet<string> DamageContainers = new();

    protected override bool WorksInHands => true; // Goobstation

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjurableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<ShowHealthIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthIconsComponent> component)
    {
        base.UpdateInternal(component);

        DamageContainers.Clear();
        foreach (var comp in component.Components)
        {
            foreach (var damageContainerId in comp.DamageContainers)
            {
                DamageContainers.Add(damageContainerId);
            }
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        DamageContainers.Clear();
    }

    private void OnHandleState(Entity<ShowHealthIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    private void OnGetStatusIconsEvent(Entity<InjurableComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        AddHealthIcons(entity, args.StatusIcons); // Arcane-Edit: DecideHealthIcons > AddHealthIcons
    }

    // Arcane-Edit-Start: This runs every frame for every visible mob, so write straight into the existing list.
    private void AddHealthIcons(Entity<InjurableComponent> entity, List<StatusIconData> result)
    {
    // Arcane-Edit-End
        var injurableComp = entity.Comp;

        if (injurableComp.DamageContainer == null ||
            !DamageContainers.Contains(injurableComp.DamageContainer))
        {
            return; // Arcane-Edit: Array.Empty > return
        }

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        if (injurableComp?.DamageContainer == "Biological")
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                // Since there is no MobState for a rotting mob, we have to deal with this case first.
                if (HasComp<RottingComponent>(entity) && ProtoMan.Resolve(injurableComp.RottingIcon, out var rottingIcon))
                    result.Add(rottingIcon);
                else if (injurableComp.HealthIcons.TryGetValue(state.CurrentState, out var value) && ProtoMan.Resolve(value, out var icon))
                    result.Add(icon);
            }
        }
    }

    // Goobstation
    protected override void OnRefreshEquipmentHud(Entity<ShowHealthIconsComponent> ent,
        ref HeldRelayedEvent<RefreshEquipmentHudEvent<ShowHealthIconsComponent>> args)
    {
        if (ent.Comp.WorksInHands)
            base.OnRefreshEquipmentHud(ent, ref args);
    }
}
