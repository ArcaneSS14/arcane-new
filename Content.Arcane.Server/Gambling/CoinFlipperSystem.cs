using Content.Arcane.Shared.Gambling;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Arcane.Server.Gambling;

/// <summary>Gambles the inserted finite cash stack once; later deposits belong to the next play.</summary>
public sealed partial class CoinFlipperSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedStackSystem _stacks = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CoinFlipperComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CoinFlipperComponent, CoinFlipperDoAfterEvent>(OnComplete);
        SubscribeLocalEvent<CoinFlipperComponent, PowerChangedEvent>(OnPower);
        SubscribeLocalEvent<CoinFlipperComponent, ComponentShutdown>(OnShutdown);
    }

    private bool Powered(Entity<CoinFlipperComponent> ent)
        => !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent)
           && HasComp<ApcPowerReceiverComponent>(ent) && _power.IsPowered(ent);

    private void OnActivate(Entity<CoinFlipperComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;
        args.Handled = true;
        if (ent.Comp.Spinning || !Powered(ent) || !_blocker.CanInteract(args.User, ent)
            || !_interaction.InRangeAndAccessible(args.User, ent.Owner)
            || !float.IsFinite(ent.Comp.DoAfterTime) || ent.Comp.DoAfterTime <= 0)
            return;

        var cash = _slots.GetItemOrNull(ent.Owner, "money");
        if (cash is not { } payment || TerminatingOrDeleted(payment) || EntityManager.IsQueuedForDeletion(payment)
            || !HasComp<CashComponent>(payment) || !TryComp<StackComponent>(payment, out var stack)
            || _stacks.IsUnlimited((payment, stack)) || _stacks.GetCount((payment, stack)) <= 0)
        {
            _popup.PopupEntity(Loc.GetString("slotmachine-no-money"), ent, args.User);
            return;
        }
        var amount = _stacks.GetCount((payment, stack));
        if (amount > int.MaxValue / 2)
        {
            _popup.PopupEntity(Loc.GetString("arcane-coinflipper-stake-too-large"), ent, args.User);
            return;
        }

        ent.Comp.Spinning = true;
        ent.Comp.Stake = amount;
        var operation = ++ent.Comp.Operation;
        var delay = new DoAfterArgs(EntityManager, ent, ent.Comp.DoAfterTime,
            new CoinFlipperDoAfterEvent { Operation = operation }, ent)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
            MultiplyDelay = false,
        };
        if (!_doAfter.TryStartDoAfter(delay, out var id))
        {
            Reset(ent);
            return;
        }
        ent.Comp.DoAfter = id;
        if (!_stacks.TryUse((payment, stack), amount))
        {
            Reset(ent);
            return;
        }
        _audio.PlayPvs(ent.Comp.SpinSound, ent);
    }

    private void OnComplete(Entity<CoinFlipperComponent> ent, ref CoinFlipperDoAfterEvent args)
    {
        if (args.Handled || !ent.Comp.Spinning || args.Operation != ent.Comp.Operation)
            return;
        args.Handled = true;
        ent.Comp.DoAfter = null;
        var amount = ent.Comp.Stake;
        var cancelled = args.Cancelled || !Powered(ent);
        Reset(ent);
        if (cancelled)
            return;

        if (!_random.Prob(0.5f))
        {
            _audio.PlayPvs(ent.Comp.LoseSound, ent);
            return;
        }

        var payout = amount * 2;
        var money = Spawn("SpaceCash", Transform(ent).Coordinates);
        _stacks.SetCount((money, null), payout);
        _audio.PlayPvs(ent.Comp.WinSound, ent);
        _chat.TrySendInGameICMessage(ent, Loc.GetString("coinflipper-win", ("amount", payout)),
            InGameICChatType.Speak, hideChat: false, hideLog: true, checkRadioPrefix: false);
    }

    private void OnPower(Entity<CoinFlipperComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Reset(ent);
    }

    private void OnShutdown(Entity<CoinFlipperComponent> ent, ref ComponentShutdown args) => Reset(ent);

    private void Reset(Entity<CoinFlipperComponent> ent)
    {
        var pending = ent.Comp.DoAfter;
        ent.Comp.DoAfter = null;
        ent.Comp.Spinning = false;
        ent.Comp.Stake = 0;
        _doAfter.Cancel(pending);
    }
}
