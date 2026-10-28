// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ToggleableHUD;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.Actions;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.ToggleableHUD;

public sealed partial class ToggleableHUDSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableHUDComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleableHUDComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ToggleableHUDComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnMapInit(Entity<ToggleableHUDComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<ToggleableHUDComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnToggle(Entity<ToggleableHUDComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggle((ent, ent.Comp));
    }

    public bool TryToggle(Entity<ToggleableHUDComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var uid = ent.Owner;
        var toggled = !ent.Comp.IsToggled;

        if (toggled)
        {
            EnsureComp<ShowJobIconsComponent>(uid);
            EnsureComp<ShowMindShieldIconsComponent>(uid);
            EnsureComp<ShowCriminalRecordIconsComponent>(uid);

            var healthBars = EnsureComp<ShowHealthBarsComponent>(uid);
            healthBars.DamageContainers = new List<ProtoId<DamageContainerPrototype>>(ent.Comp.DamageContainers);
            Dirty(uid, healthBars);
            EnsureComp<ShowHealthIconsComponent>(uid);
            EnsureComp<ShowDiseaseIconsComponent>(uid);

            EnsureComp<ShowSyndicateIconsComponent>(uid);
        }
        else
        {
            RemComp<ShowJobIconsComponent>(uid);
            RemComp<ShowMindShieldIconsComponent>(uid);
            RemComp<ShowCriminalRecordIconsComponent>(uid);

            RemComp<ShowHealthBarsComponent>(uid);
            RemComp<ShowHealthIconsComponent>(uid);
            RemComp<ShowDiseaseIconsComponent>(uid);

            RemComp<ShowSyndicateIconsComponent>(uid);
        }

        _popup.PopupEntity(Loc.GetString(toggled ? ent.Comp.PopupToggleOn : ent.Comp.PopupToggleOff), uid, uid);

        ent.Comp.IsToggled = toggled;
        Dirty(uid, ent.Comp);
        return true;
    }
}
