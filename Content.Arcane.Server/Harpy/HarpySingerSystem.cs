// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 JohnOakman <sremy2012@hotmail.fr>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.Harpy;
using Content.Server.Instruments;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.ForceSay;
using Content.Shared.FixedPoint;
using Content.Shared.Instruments;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Harpy;

public sealed partial class HarpySingerSystem : EntitySystem
{
    [Dependency] private InstrumentSystem _instrument = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    [SubscribeLocalEvent]
    private void OnStartup(EntityUid uid, HarpySingerComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.MidiAction, component.MidiActionId?.Id);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, HarpySingerComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.MidiAction);
    }

    [SubscribeLocalEvent]
    private void OnEquip(GotEquippedEvent args)
    {
        // Check if an item that makes the singer mumble is equipped to their face
        // (not their pockets!). As of writing, this should just be the muzzle.
        if (HasComp<MumbleAccentComponent>(args.Equipment) && args.Slot == "mask")
        {
            CloseMidiUi(args.EquipTarget);
        }
    }

    [SubscribeLocalEvent]
    private void OnMobStateChangedEvent(EntityUid uid, InstrumentComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
            CloseMidiUi(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnZombified(ref EntityZombifiedEvent args)
    {
        CloseMidiUi(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnStunned(EntityUid uid, InstrumentComponent component, ref StunnedEvent args)
    {
        CloseMidiUi(uid);
    }

    [SubscribeLocalEvent]
    private void OnSleep(EntityUid uid, InstrumentComponent component, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            CloseMidiUi(uid);
    }

    [SubscribeLocalEvent]
    private void OnStatusEffect(EntityUid uid, InstrumentComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Muted")
            CloseMidiUi(uid);
    }

    /// <summary>
    /// Uses the same configured damage threshold as force speech so that singing interruption
    /// remains consistent with the existing force-speech behavior.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforeDamageChanged(EntityUid uid, HarpySingerComponent component, BeforeDamageChangedEvent args)
    {
        if (!component.ShutUpDamageThreshold.HasValue || !args.Damage.AnyPositive())
            return;

        var totalApplicableDamage = FixedPoint2.Zero;
        foreach (var (_, value) in args.Damage.GetDamagePerGroup(_prototype))
            totalApplicableDamage += value;

        if (totalApplicableDamage >= component.ShutUpDamageThreshold)
            CloseMidiUi(uid);
    }

    /// <summary>
    /// Closes the MIDI UI if it is open.
    /// </summary>
    private void CloseMidiUi(EntityUid uid)
    {
        if (!HasComp<ActiveInstrumentComponent>(uid) || !TryComp<ActorComponent>(uid, out var actor))
            return;

        var attachedEntity = actor.PlayerSession.AttachedEntity;
        if (attachedEntity is not { } user)
            return;

        _instrument.ToggleInstrumentUi(uid, user);
    }

    /// <summary>
    /// Prevent the player from opening the MIDI UI under some circumstances.
    /// </summary>
    [SubscribeLocalEvent(before: [typeof(ActivatableUISystem)])]
    private void OnInstrumentOpen(EntityUid uid, HarpySingerComponent component, OpenUiActionEvent args)
    {
        var cannotSpeak = !_blocker.CanSpeak(uid);
        var zombified = HasComp<ZombieComponent>(uid);
        var muzzled = _inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                      HasComp<MumbleAccentComponent>(maskUid);

        args.Handled = cannotSpeak || muzzled || zombified;
        if (args.Handled)
            _popupSystem.PopupEntity(Loc.GetString("no-sing-while-no-speak"), uid, uid, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnBoundUIClosed(EntityUid uid, HarpySingerComponent component, BoundUIClosedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        TryComp(uid, out AppearanceComponent? appearance);
        _appearance.SetData(uid, HarpyVisualLayers.Singing, SingingVisualLayer.False, appearance);
    }

    [SubscribeLocalEvent]
    private void OnBoundUIOpened(EntityUid uid, HarpySingerComponent component, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        TryComp(uid, out AppearanceComponent? appearance);
        _appearance.SetData(uid, HarpyVisualLayers.Singing, SingingVisualLayer.True, appearance);
    }
}
