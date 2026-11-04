// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Weapons.CounterattackWeapon;

public sealed partial class CounterattackWeaponSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedItemSwitchSystem _switch = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _counterattacking = []; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CounterattackWeaponUserComponent, AttackedEvent>(OnAttacked);

        SubscribeLocalEvent<CounterattackWeaponComponent, ComponentInit>(OnStartup);
        SubscribeLocalEvent<CounterattackWeaponComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CounterattackWeaponComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CounterattackWeaponComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnAttacked(Entity<CounterattackWeaponUserComponent> ent, ref AttackedEvent args)
    {
        if (!_timing.IsFirstTimePredicted || ent.Owner == args.User || _counterattacking.Contains(ent.Owner)) // Arcane-Edit: Prevent mutually recursive counterattacks.
            return;

        var meleeWeapon = ent.Comp.Weapons.FirstOrDefault(EntityUid.Invalid);
        if (!meleeWeapon.IsValid()
            || !_hands.IsHolding(ent.Owner, meleeWeapon) // Arcane
            || !TryComp<MeleeWeaponComponent>(meleeWeapon, out var meleeComp)
            || !TryComp<CounterattackWeaponComponent>(meleeWeapon, out var counterattack))
            return;

        // Arcane-Edit-Start: Keep the guard until nested attack events and switching have finished.
        _counterattacking.Add(ent.Owner);
        try
        {
            meleeComp.NextAttack = TimeSpan.Zero;
            _melee.AttemptLightAttack(ent, meleeWeapon, meleeComp, args.User);
            meleeComp.NextAttack = TimeSpan.Zero;
            _switch.Switch(meleeWeapon, counterattack.SetItemSwitch);
            if (TryComp<UseDelayComponent>(meleeWeapon, out var delay))
                _delay.ResetAllDelays((meleeWeapon, delay));
        }
        finally
        {
            _counterattacking.Remove(ent.Owner);
        }
        // Arcane-Edit-End
    }

    private void OnStartup(Entity<CounterattackWeaponComponent> ent, ref ComponentInit args)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container)
            || !_hands.EnumerateHeld(container.Owner).Contains(ent))
            return;

        // Arcane-Edit-Start: Initialization and insertion can observe the same held weapon.
        var weapons = EnsureComp<CounterattackWeaponUserComponent>(container.Owner).Weapons;
        if (!weapons.Contains(ent.Owner))
            weapons.Add(ent.Owner);
        // Arcane-Edit-End
    }

    private void OnInserted(Entity<CounterattackWeaponComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_hands.EnumerateHeld(args.Container.Owner).Contains(ent))
            return;

        // Arcane-Edit-Start: Register each held weapon once.
        var weapons = EnsureComp<CounterattackWeaponUserComponent>(args.Container.Owner).Weapons;
        if (!weapons.Contains(ent.Owner))
            weapons.Add(ent.Owner);
        // Arcane-Edit-End
    }

    private void OnShutdown(Entity<CounterattackWeaponComponent> ent, ref ComponentShutdown args)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container)
            || !TryComp<CounterattackWeaponUserComponent>(container.Owner, out var user))
            return;

        user.Weapons.Remove(ent);
        if (user.Weapons.Count == 0)
            RemComp(container.Owner, user);
    }

    private void OnRemoved(Entity<CounterattackWeaponComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<CounterattackWeaponUserComponent>(args.Container.Owner, out var user))
            return;

        user.Weapons.Remove(ent);
        if (user.Weapons.Count == 0)
            RemComp(args.Container.Owner, user);
    }
}
