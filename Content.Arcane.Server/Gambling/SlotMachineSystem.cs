using Content.Arcane.Shared.Gambling;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Gambling;

public sealed partial class SlotMachineSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedStackSystem _stacks = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PrizeSystem _prizes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlotMachineComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SlotMachineComponent, GotEmaggedEvent>(OnEmag);
        SubscribeLocalEvent<SlotMachineComponent, PowerChangedEvent>(OnPower);
        SubscribeLocalEvent<SlotMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlotMachineComponent, DoAfterAttemptEvent<SlotMachineDoAfterEvent>>(OnAttempt);
        SubscribeLocalEvent<SlotMachineComponent, SlotMachineDoAfterEvent>(OnComplete);
    }

    private void OnActivate(Entity<SlotMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;
        args.Handled = true;
        if (!CanInteract(ent, args.User) || ent.Comp.Spinning)
            return;
        var money = _slots.GetItemOrNull(ent.Owner, "money");
        if (money is not { } cash || !ValidCash(ent, cash))
        {
            _popup.PopupEntity(Loc.GetString("slotmachine-no-money"), ent, args.User);
            return;
        }
        Start(ent, args.User, cash, false);
    }

    private void OnEmag(Entity<SlotMachineComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(ent) || ent.Comp.Spinning || !CanInteract(ent, args.UserUid))
            return;
        args.Handled = Start(ent, args.UserUid, null, true);
    }

    private bool CanInteract(Entity<SlotMachineComponent> ent, EntityUid user)
        => !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent)
           && !TerminatingOrDeleted(user) && !EntityManager.IsQueuedForDeletion(user)
           && HasComp<ApcPowerReceiverComponent>(ent) && _power.IsPowered(ent)
           && _blocker.CanInteract(user, ent)
           && _interaction.InRangeAndAccessible(user, ent.Owner);

    private bool ValidCash(Entity<SlotMachineComponent> ent, EntityUid cash)
        => ent.Comp.SpinCost > 0 && !TerminatingOrDeleted(cash) && !EntityManager.IsQueuedForDeletion(cash)
           && _slots.GetItemOrNull(ent.Owner, "money") == cash && HasComp<CashComponent>(cash)
           && TryComp<StackComponent>(cash, out var stack) && !_stacks.IsUnlimited((cash, stack))
           && _stacks.GetCount((cash, stack)) >= ent.Comp.SpinCost;

    private bool Start(Entity<SlotMachineComponent> ent, EntityUid user, EntityUid? cash, bool emag)
    {
        // Positive delays cannot complete synchronously before the stake is paid.
        if (!float.IsFinite(ent.Comp.DoAfterTime) || ent.Comp.DoAfterTime <= 0)
            return false;
        ent.Comp.Spinning = true;
        ent.Comp.User = user;
        ent.Comp.UserId = TryComp<ActorComponent>(user, out var actor) ? actor.PlayerSession.UserId : null;
        ent.Comp.EmagSpin = emag;
        var operation = ++ent.Comp.Operation;
        _appearance.SetData(ent, SlotMachineVisuals.Spinning, true);
        var args = new DoAfterArgs(EntityManager, ent, ent.Comp.DoAfterTime,
            new SlotMachineDoAfterEvent { Operation = operation }, ent)
        {
            RequireCanInteract = false,
            DistanceThreshold = null,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BlockDuplicate = false,
            CancelDuplicate = false,
            MultiplyDelay = false,
        };
        if (!_doAfter.TryStartDoAfter(args, out var id))
        {
            if (ent.Comp.Operation == operation && ent.Comp.Spinning)
                Reset(ent);
            return false;
        }
        ent.Comp.DoAfter = id;
        // Goob commits the stake when the spin starts; leaving or removing the remaining cash cannot cancel it.
        if (!emag && (cash is not { } payment || !ValidCash(ent, payment)
                      || !_stacks.TryUse(payment, ent.Comp.SpinCost)))
        {
            Reset(ent);
            return false;
        }
        if (ent.Comp.SpinSound != null)
            _audio.PlayPvs(ent.Comp.SpinSound, ent);
        return true;
    }

    private bool ValidOperation(Entity<SlotMachineComponent> ent)
        => ent.Comp.Spinning && !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent)
           && HasComp<ApcPowerReceiverComponent>(ent) && _power.IsPowered(ent);

    private void OnAttempt(Entity<SlotMachineComponent> ent, ref DoAfterAttemptEvent<SlotMachineDoAfterEvent> args)
    {
        if (args.Event.Operation != ent.Comp.Operation || !ValidOperation(ent))
            args.Cancel();
    }

    private void OnComplete(Entity<SlotMachineComponent> ent, ref SlotMachineDoAfterEvent args)
    {
        if (args.Handled || !ent.Comp.Spinning || args.Operation != ent.Comp.Operation)
            return;
        args.Handled = true;
        ent.Comp.DoAfter = null;
        if (args.Cancelled || !ValidOperation(ent))
        {
            Reset(ent);
            return;
        }

        EntityUid? user = !ent.Comp.EmagSpin && ent.Comp.User is { } original
                         && TryComp<ActorComponent>(original, out var actor)
                         && actor.PlayerSession.UserId == ent.Comp.UserId ? original : null;
        var prize = _prizes.Pick(ent.Comp.Prizes);
        if (prize == null)
        {
            Reset(ent);
            return;
        }
        Reset(ent);
        if (!TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent))
            _prizes.Award(prize, ent, user);
    }

    private void OnPower(Entity<SlotMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Reset(ent);
    }

    private void OnShutdown(Entity<SlotMachineComponent> ent, ref ComponentShutdown args) => Reset(ent);

    private void Reset(Entity<SlotMachineComponent> ent)
    {
        if (!ent.Comp.Spinning)
            return;
        var id = ent.Comp.DoAfter;
        ent.Comp.DoAfter = null;
        ent.Comp.Spinning = false;
        ent.Comp.User = null;
        ent.Comp.UserId = null;
        ent.Comp.EmagSpin = false;
        _doAfter.Cancel(id);
        if (!TerminatingOrDeleted(ent))
            _appearance.SetData(ent, SlotMachineVisuals.Spinning, false);
    }
}
