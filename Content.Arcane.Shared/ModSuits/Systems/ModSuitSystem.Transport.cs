// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Body.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Metabolism;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedEntityStorageSystem _transportStorage = default!;
    [Dependency] private PullingSystem _transportPulling = default!;
    [Dependency] private MetabolizerSystem _transportMetabolism = default!;
    [Dependency] private SharedStaminaSystem _transportStamina = default!;
    private readonly List<Entity<ModHardlightBagComponent>> _pendingTransportPulls = [];

    private void InitializeTransport()
    {
        SubscribeLocalEvent<ModPatientTransportComponent, AfterInteractEvent>(OnProjectorInteract);
        SubscribeLocalEvent<ModPatientTransportComponent, ModPatientTransportDoAfterEvent>(OnTransportFinished);
        SubscribeLocalEvent<ModPatientTransportComponent, EntityTerminatingEvent>(OnProjectorTerminating);
        SubscribeLocalEvent<ModHardlightBagComponent, PullStoppedMessage>(OnBagPullStopped);
        SubscribeLocalEvent<ModHardlightBagComponent, StorageOpenAttemptEvent>(OnBagOpenAttempt);
        SubscribeLocalEvent<ModHardlightBagComponent, StorageAfterOpenEvent>(OnBagOpened);
        SubscribeLocalEvent<ModHardlightBagComponent, FoldAttemptEvent>(OnBagFoldAttempt);
        SubscribeLocalEvent<ModHardlightBagComponent, EntityTerminatingEvent>(OnBagTerminating);
        SubscribeLocalEvent<ModHardlightBagComponent, EntRemovedFromContainerMessage>(OnBagOccupantRemoved);
        SubscribeLocalEvent<ModTransportStasisComponent, GetMetabolicMultiplierEvent>(OnTransportMetabolism);
    }

    private bool CanUseProjector(EntityUid projector, EntityUid wearer) =>
        TryComp(projector, out ModDeviceItemComponent? item)
        && TryComp(item.Module, out ModModuleComponent? module) && module.Active
        && TryComp(module.Suit, out ModSuitComponent? suit) && suit.Wearer == wearer
        && _hands.IsHolding(wearer, projector) && _actionBlocker.CanInteract(wearer, projector);

    private bool CanTransport(EntityUid wearer, EntityUid target) =>
        wearer != target && !TerminatingOrDeleted(target) && HasComp<MobStateComponent>(target)
        && !_containers.IsEntityInContainer(target) && _interaction.InRangeUnobstructed(wearer, target);

    private void OnProjectorInteract(Entity<ModPatientTransportComponent> projector, ref AfterInteractEvent args)
    {
        if (_net.IsClient || args.Handled || !args.CanReach || args.Target is not { } target
            || !CanUseProjector(projector, args.User))
            return;

        if (target == projector.Comp.Bag)
        {
            args.Handled = true;
            ReleaseTransportBag(projector);
            return;
        }

        if (!CanTransport(args.User, target))
            return;

        args.Handled = true;
        if (projector.Comp.Bag != null)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-transport-already-linked"), projector, args.User);
            return;
        }

        _clampDoAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, projector.Comp.CaptureDelay,
            new ModPatientTransportDoAfterEvent(), projector, target, projector)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnTransportFinished(Entity<ModPatientTransportComponent> projector, ref ModPatientTransportDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || args.Target is not { } target
            || projector.Comp.Bag != null || !CanUseProjector(projector, args.User) || !CanTransport(args.User, target))
            return;

        var uid = SpawnAttachedTo(projector.Comp.BagPrototype, Transform(target).Coordinates);
        if (!TryComp(uid, out ModHardlightBagComponent? bag)
            || !TryComp(uid, out EntityStorageComponent? storage) || storage.Open
            || !_transportStorage.Insert(target, uid, storage))
        {
            QueueDel(uid);
            return;
        }

        args.Handled = true;
        projector.Comp.Bag = uid;
        bag.Projector = projector;
        bag.Wearer = args.User;
        bag.Occupant = target;
        // Replicate the new bag before it shares a physics joint with an existing entity.
        bag.PullAt = _timing.CurTime + _timing.TickPeriod * 3;
        if (bag.Capture)
        {
            if (!HasComp<MumbleAccentComponent>(target))
                bag.AddedAccent = AddComp<MumbleAccentComponent>(target);
        }
        else
        {
            var stasis = EnsureComp<ModTransportStasisComponent>(target);
            stasis.Bag = uid;
            stasis.Multiplier = bag.MetabolicMultiplier;
            _transportMetabolism.UpdateMetabolicMultiplier(target);
        }

        _audio.PlayPvs(projector.Comp.Sound, uid);
        _pendingTransportPulls.Add((uid, bag));
    }

    private void UpdateTransport()
    {
        for (var i = _pendingTransportPulls.Count - 1; i >= 0; i--)
        {
            var bag = _pendingTransportPulls[i];
            if (bag.Comp.PullAt is { } time && time > _timing.CurTime
                && !TerminatingOrDeleted(bag) && !bag.Comp.Releasing)
                continue;

            _pendingTransportPulls.RemoveAt(i);
            if (TerminatingOrDeleted(bag) || bag.Comp.Releasing)
                continue;

            bag.Comp.PullAt = null;
            if (bag.Comp.Projector is not { } projector || bag.Comp.Wearer is not { } wearer
                || !CanUseProjector(projector, wearer) || !_transportPulling.TryStartPull(wearer, bag))
                DissolveBag(bag);
        }
    }

    private void OnBagPullStopped(EntityUid uid, ModHardlightBagComponent bag, PullStoppedMessage args)
    {
        if (_net.IsServer && args.PulledUid == uid)
            DissolveBag((uid, bag));
    }

    private void OnBagOpenAttempt(Entity<ModHardlightBagComponent> bag, ref StorageOpenAttemptEvent args)
    {
        if (bag.Comp.Capture)
            args.Cancelled = true;
    }

    private void OnBagFoldAttempt(Entity<ModHardlightBagComponent> bag, ref FoldAttemptEvent args) =>
        args.Cancelled = true;

    private void OnBagOpened(Entity<ModHardlightBagComponent> bag, ref StorageAfterOpenEvent args)
    {
        if (_net.IsServer)
            DissolveBag(bag);
    }

    private void OnProjectorTerminating(Entity<ModPatientTransportComponent> projector, ref EntityTerminatingEvent args) =>
        ReleaseTransportBag(projector);

    private void ReleaseTransportBag(EntityUid projector)
    {
        if (_net.IsServer && TryComp(projector, out ModPatientTransportComponent? device)
            && device.Bag is { } uid && TryComp(uid, out ModHardlightBagComponent? bag))
            DissolveBag((uid, bag));
    }

    private void OnBagTerminating(Entity<ModHardlightBagComponent> bag, ref EntityTerminatingEvent args)
    {
        if (_net.IsServer)
            DissolveBag(bag, terminating: true);
    }

    private void DissolveBag(Entity<ModHardlightBagComponent> bag, bool terminating = false)
    {
        if (bag.Comp.Releasing)
            return;

        bag.Comp.Releasing = true;
        if (TryComp(bag.Comp.Projector, out ModPatientTransportComponent? device) && device.Bag == bag.Owner)
        {
            device.Bag = null;
            if (!terminating)
                _audio.PlayPvs(device.Sound, bag);
        }

        if (TryComp(bag, out EntityStorageComponent? storage))
        {
            foreach (var occupant in new List<EntityUid>(storage.Contents.ContainedEntities))
            {
                if (TerminatingOrDeleted(occupant))
                    continue;

                if (!_transportStorage.Remove(occupant, bag, storage))
                {
                    var destination = _transform.GetMoverCoordinates(bag);
                    if (destination.EntityId != bag.Owner && destination.IsValid(EntityManager))
                        _containers.Remove(occupant, storage.Contents, force: true, destination: destination);
                    else
                        _transform.DetachEntity(occupant);

                    if (TryComp(occupant, out InsideEntityStorageComponent? inside) && inside.Storage == bag.Owner)
                        RemComp(occupant, inside);
                }
            }
        }

        if (!terminating)
            QueueDel(bag);
    }

    private void OnBagOccupantRemoved(Entity<ModHardlightBagComponent> bag, ref EntRemovedFromContainerMessage args)
    {
        if (_net.IsClient || args.Container.ID != SharedEntityStorageSystem.ContainerName || bag.Comp.Occupant != args.Entity)
            return;

        var occupant = args.Entity;
        bag.Comp.Occupant = null;
        if (!TerminatingOrDeleted(occupant))
        {
            if (TryComp(occupant, out ModTransportStasisComponent? stasis) && stasis.Bag == bag.Owner)
            {
                RemComp(occupant, stasis);
                _transportMetabolism.UpdateMetabolicMultiplier(occupant);
            }

            if (bag.Comp.AddedAccent is { } accent && TryComp(occupant, out MumbleAccentComponent? current) && accent == current)
                RemComp(occupant, accent);

            if (bag.Comp.Capture && TryComp(occupant, out StaminaComponent? stamina))
                _transportStamina.TakeStaminaDamage(occupant, stamina.CritThreshold, stamina, ignoreResist: true);
        }

        // Component and container removal may both fire during an opening or destruction.
        // Delay cleanup until the storage finishes its own removal.
        if (!bag.Comp.Releasing && !TerminatingOrDeleted(bag))
            QueueDel(bag);
    }

    private void OnTransportMetabolism(Entity<ModTransportStasisComponent> stasis, ref GetMetabolicMultiplierEvent args) =>
        args.Multiplier *= stasis.Comp.Multiplier;
}
