// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.Power;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Trauma.Shared.Effects;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.Power;

public sealed partial class InducerSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SparksSystem _sparks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InducerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<InducerComponent, InducerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InducerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<InducerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnAfterInteract(Entity<InducerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-battery"), ent, args.User);
            return;
        }

        if (GetCell(ent) is not { } cell)
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-power-cell"), ent, args.User);
            return;
        }

        if (_battery.GetCharge(cell.AsNullable()) <= 0)
        {
            _popup.PopupEntity(Loc.GetString("inducer-empty"), ent, args.User);
            return;
        }

        if (_battery.IsFull((target, targetBattery)))
        {
            _popup.PopupEntity(Loc.GetString("inducer-target-full"), ent, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.TransferDelay, new InducerDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            DistanceThreshold = ent.Comp.MaxDistance,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<InducerComponent> ent, ref InducerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<BatteryComponent>(target, out var targetBattery) || GetCell(ent) is not { } cell)
            return;

        var multiplier = UsesStructureMultiplier(target)
            ? ent.Comp.StructureTransferMultiplier
            : ent.Comp.TransferMultiplier;

        if (multiplier <= 0f)
            return;

        var consume = Math.Min(ent.Comp.TransferRate * ent.Comp.TransferDelay, _battery.GetCharge(cell.AsNullable()));
        var free = targetBattery.MaxCharge - _battery.GetCharge((target, targetBattery));
        var receive = Math.Min(consume * multiplier, free);
        if (receive <= 0f)
            return;

        if (!_battery.TryUseCharge(cell.AsNullable(), receive / multiplier))
            return;

        _battery.ChangeCharge((target, targetBattery), receive);
        _sparks.DoSparks(Transform(target).Coordinates);

        args.Repeat = !_battery.IsFull((target, targetBattery));
    }

    private void OnUseInHand(Entity<InducerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var rates = ent.Comp.AvailableTransferRates;
        if (rates.Count == 0)
            return;

        var next = rates[(rates.IndexOf(ent.Comp.TransferRate) + 1) % rates.Count];
        SetRate(ent, next, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<InducerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var priority = 0;
        foreach (var rate in ent.Comp.AvailableTransferRates)
        {
            args.Verbs.Add(new Verb
            {
                Category = VerbCategory.SelectType,
                Text = Loc.GetString("inducer-set-transfer-rate", ("rate", rate)),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Priority = priority--,
                Act = () => SetRate(ent, rate, user),
            });
        }
    }

    private void SetRate(Entity<InducerComponent> ent, int rate, EntityUid user)
    {
        if (ent.Comp.TransferRate == rate)
            return;

        ent.Comp.TransferRate = rate;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("inducer-transfer-rate-set", ("rate", rate)), ent, user);
    }

    private Entity<BatteryComponent>? GetCell(Entity<InducerComponent> ent)
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.PowerCellSlotId, out var slot)
            || slot.Item is not { } item
            || !TryComp<BatteryComponent>(item, out var battery))
            return null;

        return (item, battery);
    }

    private bool UsesStructureMultiplier(EntityUid target)
    {
        return Transform(target).Anchored
            || HasComp<ApcComponent>(target)
            || HasComp<MachineComponent>(target)
            || HasComp<WallMountComponent>(target);
    }
}
