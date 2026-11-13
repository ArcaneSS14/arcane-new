// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Components.Equipment;
using Content.Medical.Common.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Trauma.Common.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

// Arcane-Start
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
// Arcane-End

namespace Content.Goobstation.Shared.Xenobiology;

// This handles any actions that slime mobs may have.
public sealed partial class SlimeLatchSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery = default!;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery = default!;
    [Dependency] private EntityQuery<XenoVacuumTankComponent> _tankQuery = default!;

    // Arcane-Start
    [Dependency] private BodySystem _body = default!;
    [Dependency] private StomachSystem _stomach = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private INetManager _net = default!;
    // Arcane-End

    private TimeSpan _updateDelay = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + _updateDelay;

        var query = EntityQueryEnumerator<SlimeDamageOvertimeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid) ||
                // incase some bullshit doesnt clean it up properly
                comp.SourceEntityUid is not { } source ||
                !_slimeQuery.TryComp(source, out var slime) ||
                slime.LatchedTarget != uid)
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            UpdateHunger((uid, comp), source);
        }
    }

    private void UpdateHunger(Entity<SlimeDamageOvertimeComponent> ent, EntityUid source)
    {
        _damageable.ChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: true, targetPart: TargetBodyPart.All);

        // Arcane-Edit-Start: only accepted blood feeds the slime
        if (_net.IsClient || TerminatingOrDeleted(ent) || _mobState.IsDead(ent))
            return;

        var fed = FeedOnBlood(ent, source);
        if (fed > 0 && _satiationQuery.TryComp(source, out var satiation))
            _satiation.ModifyValue((source, satiation), SatiationSystem.Hunger, (float) fed);
        // Arcane-Edit-End
    }

    // Arcane-Start
    private FixedPoint2 FeedOnBlood(Entity<SlimeDamageOvertimeComponent> ent, EntityUid source)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream)
            || !_solutions.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var blood)
            || bloodstream.BloodSolution is not { } bloodEntity
            || blood.Volume <= 0)
            return FixedPoint2.Zero;

        var accepted = FixedPoint2.Zero;
        foreach (var stomach in _body.GetOrgans<StomachComponent>(source))
        {
            if (!_solutions.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp.Solution, out var stomachContents))
                continue;

            var amount = FixedPoint2.Min(ent.Comp.SuctionUnits - accepted, stomachContents.AvailableVolume);
            if (amount <= 0)
                continue;

            var portion = _solutions.SplitSolutionWithout(bloodEntity, amount, ent.Comp.ToxinReagent);
            if (portion.Volume <= 0)
                break;

            var portionVolume = portion.Volume;
            if (!_stomach.TryTransferSolution((stomach.Owner, stomach.Comp, null), portion))
            {
                _solutions.ForceAddSolution(bloodEntity, portion);
                continue;
            }

            accepted += portionVolume;
            if (accepted >= ent.Comp.SuctionUnits)
                break;
        }

        if (accepted > 0)
            _solutions.TryAddReagent(bloodEntity, ent.Comp.ToxinReagent.Id, ent.Comp.ToxinUnits);

        return accepted;
    }

    [SubscribeLocalEvent]
    private void OnSlimeTerminating(Entity<SlimeComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.PendingLatchTarget is { } pending)
            ReleaseLatchReservation(ent, pending);

        if (ent.Comp.LatchedTarget is { } target)
        {
            ReleaseLatchReservation(ent, target);
            if (TryComp<SlimeDamageOvertimeComponent>(target, out var damage) && damage.SourceEntityUid == ent.Owner)
                RemCompDeferred(target, damage);
        }
    }

    [SubscribeLocalEvent]
    private void OnVictimTerminating(Entity<SlimeDamageOvertimeComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.SourceEntityUid is { } source && _slimeQuery.TryComp(source, out var slime) && !TerminatingOrDeleted(source))
            Unlatch((source, slime));
    }
    // Arcane-End

    [SubscribeLocalEvent]
    private void OnMobStateChangedSOD(Entity<SlimeDamageOvertimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.SourceEntityUid is not {} source)
            return;

        if (_slimeQuery.TryComp(source, out var slime))
            Unlatch((source, slime));
    }

    [SubscribeLocalEvent]
    private void OnMobStateChangedSlime(Entity<SlimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Unlatch(ent);
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<SlimeComponent> ent, ref PullAttemptEvent args)
    {
        if (IsLatched(ent) && args.PullerUid != ent.Owner) // slimes can't be pulled when latched
        {
            args.Cancelled = true;
            return;
        }

        Unlatch(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeingThrownAttempt(Entity<SlimeComponent> ent, ref BeingThrownAttemptEvent args)
    {
        // can't just shove a slime off, use the doafter
        args.Cancelled |= IsLatched(ent);
    }

    [SubscribeLocalEvent]
    private void OnUpdateCanMove(Entity<SlimeComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (IsLatched(ent))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnRemovedFromContainer(Entity<SlimeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // this check is probably useless but jic
        if (!_tankQuery.HasComp(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    [SubscribeLocalEvent]
    private void OnInsertedIntoContainer(Entity<SlimeComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_tankQuery.HasComp(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    [SubscribeLocalEvent]
    private void OnLatchAttempt(Entity<SlimeComponent> ent, ref SlimeLatchEvent args)
    {
        // TODO: just subscribe for SlimeComponent bruh
        var user = args.Performer;
        if (TerminatingOrDeleted(args.Target))
            return;

        if (IsLatched(ent))
        {
            Unlatch(ent);
            return;
        }

        if (CanLatch(ent, args.Target))
        {
            StartSlimeLatchDoAfter(ent, args.Target);
            return;
        }

        // improvement space (tm)
    }

    private bool StartSlimeLatchDoAfter(Entity<SlimeComponent> ent, EntityUid target)
    {
        // Arcane-Start
        if (ent.Comp.PendingLatchTarget != null || HasComp<BeingLatchedComponent>(target) || HasComp<SlimeDamageOvertimeComponent>(target))
            return false;
        // Arcane-End
        if (_mobState.IsDead(target))
        {
            var targetDeadPopup = Loc.GetString("slime-latch-fail-target-dead", ("ent", target));
            _popup.PopupEntity(targetDeadPopup, ent, ent);

            return false;
        }

        if (ent.Comp.Stomach.Count >= ent.Comp.MaxContainedEntities)
        {
            var maxEntitiesPopup = Loc.GetString("slime-latch-fail-max-entities", ("ent", target));
            _popup.PopupEntity(maxEntitiesPopup, ent, ent);

            return false;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.LatchDoAfterDuration, new SlimeLatchDoAfterEvent(), ent, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        // Arcane-Edit-Start: support synchronous completion and undo a rejected start
        var reservation = EnsureComp<BeingLatchedComponent>(target);
        reservation.Slime = ent;
        Dirty(target, reservation);
        ent.Comp.PendingLatchTarget = target;
        Dirty(ent);

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            ent.Comp.PendingLatchTarget = null;
            Dirty(ent);
            ReleaseLatchReservation(ent, target);
            return false;
        }

        if (ent.Comp.PendingLatchTarget == null)
            return true;

        var attemptPopup = Loc.GetString("slime-latch-attempt", ("slime", ent), ("ent", target));
        _popup.PopupEntity(attemptPopup, ent, ent, PopupType.MediumCaution);
        // Arcane-Edit-End
        return true;
    }

    [SubscribeLocalEvent]
    private void OnSlimeLatchDoAfter(Entity<SlimeComponent> ent, ref SlimeLatchDoAfterEvent args)
    {
        if (args.Target is not { } target || ent.Comp.PendingLatchTarget != target) // Arcane-Edit
            return;

        // Arcane-Start
        ent.Comp.PendingLatchTarget = null;
        Dirty(ent);
        // Arcane-End
        if (args.Handled || args.Cancelled || !CanLatch(ent, target)) // Arcane-Edit: the target may have moved or died
        {
            ReleaseLatchReservation(ent, target); // Arcane-Edit
            return;
        }

        // Arcane-Start
        if (!TryComp<BeingLatchedComponent>(target, out var reservation) || reservation.Slime != ent.Owner)
            return;
        // Arcane-End

        Latch(ent, target);
        args.Handled = true;
    }

    #region Helpers

    public bool IsLatched(Entity<SlimeComponent> ent)
        => ent.Comp.LatchedTarget.HasValue;

    public bool IsLatched(Entity<SlimeComponent> ent, EntityUid target)
        => ent.Comp.LatchedTarget == target;

    public bool CanLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        return !(IsLatched(ent) // already latched
            || ent.Owner == target // Arcane
            || TerminatingOrDeleted(target) // Arcane
            || !_interaction.InRangeUnobstructed(ent.Owner, target, 1f) // Arcane
            || _mobState.IsDead(target) // target dead
            || !_actionBlocker.CanInteract(ent, target) // can't reach
            || !HasComp<MobStateComponent>(target)); // make any mob work
    }

    public bool NpcTryLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (!CanLatch(ent, target))
            return false;

        return StartSlimeLatchDoAfter(ent, target);
    }

    private void Latch(Entity<SlimeComponent> ent, EntityUid target) // Arcane-Edit: require a completed latch attempt
    {
        if (IsLatched(ent))
            Unlatch(ent);

        _xform.SetCoordinates(ent, Transform(target).Coordinates);
        _xform.SetParent(ent, target);

        ent.Comp.LatchedTarget = target;
        Dirty(ent);

        _actionBlocker.UpdateCanMove(ent.Owner);

        EnsureComp(target, out SlimeDamageOvertimeComponent comp);
        comp.SourceEntityUid = ent;
        Dirty(target, comp);

        _audio.PlayPredicted(ent.Comp.EatSound, ent, ent);
        _popup.PopupEntity(Loc.GetString("slime-action-latch-success", ("slime", ent), ("target", target)), ent, ent, PopupType.SmallCaution);

        // We also need to set a new state for the slime when it's consuming,
        // this will be easy however it's important to take MobGrowthSystem into account... possibly we should use layers?
    }

    public void Unlatch(Entity<SlimeComponent> ent)
    {
        if (ent.Comp.LatchedTarget is not { } target)
            return;

        // Arcane-Edit-Start: only this slime can clear its latch and damage effect
        ReleaseLatchReservation(ent, target);
        if (TryComp<SlimeDamageOvertimeComponent>(target, out var damage) && damage.SourceEntityUid == ent.Owner)
            RemCompDeferred(target, damage);
        // Arcane-Edit-End

        // Arcane-Edit-Start: the entire grid can be terminating with the victim
        if (TryComp(target, out TransformComponent? targetTransform) && !TerminatingOrDeleted(targetTransform.ParentUid))
            _xform.SetParent(ent, targetTransform.ParentUid);
        // Arcane-Edit-End
        ent.Comp.LatchedTarget = null;
        Dirty(ent);
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    // Arcane-Start
    private void ReleaseLatchReservation(EntityUid slime, EntityUid target)
    {
        if (TryComp<BeingLatchedComponent>(target, out var reservation) && reservation.Slime == slime)
            RemCompDeferred(target, reservation);
    }
    // Arcane-End

    #endregion
}
