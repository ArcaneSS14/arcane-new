using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string HolsterContainerId = "mod-holster";

    private void InitializeHolsterModules()
    {
        SubscribeLocalEvent<ModHolsterModuleComponent, ComponentInit>(OnHolsterInit);
        SubscribeLocalEvent<ModHolsterModuleComponent, ModModuleUsedEvent>(OnHolsterUsed);
        SubscribeLocalEvent<ModHolsterModuleComponent, ModModuleRemovedEvent>(OnHolsterRemoved);
        SubscribeLocalEvent<ModHolsterModuleComponent, EntityTerminatingEvent>(OnHolsterTerminating);
    }

    private void OnHolsterInit(Entity<ModHolsterModuleComponent> module, ref ComponentInit args) =>
        module.Comp.Container = _containers.EnsureContainer<ContainerSlot>(module, HolsterContainerId);

    private void OnHolsterUsed(Entity<ModHolsterModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (module.Comp.Container.ContainedEntity is { } weapon)
        {
            if (!_hands.TryPickupAnyHand(user, weapon))
            {
                _popup.PopupEntity(Loc.GetString("modsuit-module-hands-full"), user, user);
                return;
            }

            _audio.PlayPvs(module.Comp.DrawSound, user);
        }
        else if (_hands.TryGetActiveItem(user, out var held) && HasComp<GunComponent>(held)
                 && !HasComp<ModDeviceItemComponent>(held) && _hands.CanDrop(user, held.Value)
                 && _containers.Insert(held.Value, module.Comp.Container))
        {
            _audio.PlayPvs(module.Comp.HolsterSound, user);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("modsuit-holster-requires-gun"), user, user);
            return;
        }

        args.Handled = true;
    }

    private void OnHolsterRemoved(Entity<ModHolsterModuleComponent> module, ref ModModuleRemovedEvent args) =>
        EmptyHolster(module);

    private void OnHolsterTerminating(Entity<ModHolsterModuleComponent> module, ref EntityTerminatingEvent args) =>
        EmptyHolster(module);

    private void EmptyHolster(Entity<ModHolsterModuleComponent> module)
    {
        if (!_net.IsClient && module.Comp.Container.ContainedEntity is { } item && !TerminatingOrDeleted(item))
            _containers.Remove(item, module.Comp.Container, force: true, destination: _transform.GetMoverCoordinates(module));
    }
}
