using Content.Arcane.Common.Thunderdome;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Thunderdome;

public sealed partial class ThunderdomeSystem
{
    [Dependency] private SharedGunSystem _guns = default!;
    [Dependency] private SharedPopupSystem _popups = default!;

    private void OnArenaDeath(Entity<ThunderdomePlayerComponent> victim, EntityUid? origin)
    {
        if (origin is not { } killer || killer == victim.Owner ||
            !TryComp<ThunderdomePlayerComponent>(killer, out var fighter) || fighter.Closing || fighter.Rule != victim.Comp.Rule)
            return;

        fighter.Streak++;
        if (fighter.Streak is >= 3 and <= 9 or 12)
            _popups.PopupEntity(Loc.GetString($"thunderdome-streak-{fighter.Streak}", ("player", Name(killer))), killer, PopupType.Large);

        if (_configuration.GetCVar(ThunderdomeCVars.Refill))
            RefillEquipment(killer);
    }

    private void RefillEquipment(EntityUid parent)
    {
        if (!TryComp<ContainerManagerComponent>(parent, out var manager))
            return;

        foreach (var container in _containers.GetAllContainers(parent, manager))
        {
            if (container.ID is "gun_magazine" or "gun_chamber" or "revolver-ammo")
                continue;

            foreach (var item in container.ContainedEntities)
            {
                if (!HasComp<ThunderdomeArenaItemComponent>(item))
                {
                    RefillEquipment(item);
                    continue;
                }

                if (TryComp<BallisticAmmoProviderComponent>(item, out var ballistic))
                    _guns.RefillArenaBallisticAmmo((item, ballistic));

                if (TryComp<RevolverAmmoProviderComponent>(item, out var revolver))
                    _guns.RefillArenaRevolverAmmo((item, revolver));

                if (HasComp<BatteryAmmoProviderComponent>(item))
                {
                    var charge = new GetChargeEvent();
                    RaiseLocalEvent(item, ref charge);
                    if (charge.MaxCharge > charge.CurrentCharge)
                    {
                        var refill = new ChangeChargeEvent(charge.MaxCharge - charge.CurrentCharge);
                        RaiseLocalEvent(item, ref refill);
                    }
                }

                RefillEquipment(item);
            }
        }
    }
}
