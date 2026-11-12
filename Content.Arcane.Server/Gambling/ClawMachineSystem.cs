using Content.Arcane.Shared.Gambling;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Gambling;

public sealed partial class ClawMachineSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PrizeSystem _prizes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClawMachineComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ClawMachineComponent, GotEmaggedEvent>(OnEmag);
        SubscribeLocalEvent<ClawMachineComponent, PowerChangedEvent>(OnPower);
        SubscribeLocalEvent<ClawMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ClawMachineComponent, ClawMachineDoAfterEvent>(OnComplete);
    }

    private void OnActivate(Entity<ClawMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;
        args.Handled = true;
        var user = args.User;
        if (ent.Comp.Spinning || !ValidMachine(ent)
            || TerminatingOrDeleted(user) || EntityManager.IsQueuedForDeletion(user)
            || !_blocker.CanInteract(user, ent) || !_interaction.InRangeAndAccessible(user, ent.Owner)
            || !float.IsFinite(ent.Comp.DoAfterTime) || ent.Comp.DoAfterTime <= 0)
            return;

        ent.Comp.Spinning = true;
        ent.Comp.User = user;
        ent.Comp.UserId = TryComp<ActorComponent>(user, out var actor) ? actor.PlayerSession.UserId : null;
        ent.Comp.Mind = _mind.TryGetMind(user, out var mind, out _) ? mind : null;
        var operation = ++ent.Comp.Operation;
        SetSpinning(ent, true);
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfterTime,
            new ClawMachineDoAfterEvent { Operation = operation }, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MultiplyDelay = false,
        };
        if (!_doAfter.TryStartDoAfter(doAfter, out var id))
        {
            if (ent.Comp.Spinning && ent.Comp.Operation == operation)
                Reset(ent);
            return;
        }
        // Instant DoAfters may finish inside TryStartDoAfter.
        if (ent.Comp.Spinning && ent.Comp.Operation == operation)
        {
            ent.Comp.DoAfter = id;
            _audio.PlayPvs(ent.Comp.PlaySound, ent);
        }
    }

    private void OnEmag(Entity<ClawMachineComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(ent))
            return;
        EnsureComp<EmaggedComponent>(ent);
        args.Handled = true;
    }

    private bool ValidMachine(Entity<ClawMachineComponent> ent)
        => !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent)
           && HasComp<ApcPowerReceiverComponent>(ent) && _power.IsPowered(ent);

    private void OnComplete(Entity<ClawMachineComponent> ent, ref ClawMachineDoAfterEvent args)
    {
        if (args.Handled || !ent.Comp.Spinning || args.Operation != ent.Comp.Operation)
            return;
        args.Handled = true;
        ent.Comp.DoAfter = null;
        var user = ent.Comp.User;
        if (args.Cancelled || !ValidMachine(ent) || user is not { } player
            || TerminatingOrDeleted(player) || EntityManager.IsQueuedForDeletion(player))
        {
            Reset(ent);
            if (args.Cancelled && user is { } cancelledUser && !TerminatingOrDeleted(cancelledUser))
            {
                var self = Loc.GetString("clawmachine-fail-self");
                var others = Loc.GetString("clawmachine-fail-other", ("user", cancelledUser));
                _popup.PopupEntity(self, others, cancelledUser, cancelledUser);
            }
            return;
        }

        EntityUid? credited = ent.Comp.UserId is { } userId && ent.Comp.Mind is { } originalMind
                              && TryComp<ActorComponent>(player, out var actor)
                              && actor.PlayerSession.UserId == userId
                              && _mind.TryGetMind(player, out var currentMind, out _)
                              && currentMind == originalMind ? player : null;
        var evil = HasComp<EmaggedComponent>(ent);
        var prize = _prizes.Pick(evil ? ent.Comp.EvilPrizes : ent.Comp.Prizes);
        Reset(ent);
        if (prize != null && ValidMachine(ent))
        {
            var rewards = _prizes.Award(prize, ent, credited);
            if (evil)
            {
                foreach (var item in rewards)
                    EnsureComp<EvilClawPrizeComponent>(item);
            }
        }
    }

    private void OnPower(Entity<ClawMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Reset(ent);
    }

    private void OnShutdown(Entity<ClawMachineComponent> ent, ref ComponentShutdown args) => Reset(ent);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ClawMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Spinning && (component.User is not { } user
                || TerminatingOrDeleted(user) || EntityManager.IsQueuedForDeletion(user)
                || !HasComp<DoAfterComponent>(user)))
                Reset((uid, component));
        }
    }

    private void Reset(Entity<ClawMachineComponent> ent)
    {
        if (!ent.Comp.Spinning)
            return;
        var id = ent.Comp.DoAfter;
        ent.Comp.DoAfter = null;
        ent.Comp.Spinning = false;
        ent.Comp.User = null;
        ent.Comp.Mind = null;
        ent.Comp.UserId = null;
        _doAfter.Cancel(id);
        if (!TerminatingOrDeleted(ent))
            SetSpinning(ent, false);
    }

    private void SetSpinning(Entity<ClawMachineComponent> ent, bool spinning)
    {
        _appearance.SetData(ent, ClawMachineVisuals.Spinning, spinning);
        _appearance.SetData(ent, ClawMachineVisuals.NormalSprite, !spinning);
    }
}
