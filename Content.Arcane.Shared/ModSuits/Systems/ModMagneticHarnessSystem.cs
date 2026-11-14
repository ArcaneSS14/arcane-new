using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModMagneticHarnessSystem : VirtualController
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly HashSet<EntityUid> _catch = [];
    private readonly HashSet<EntityUid> _forget = [];

    [SubscribeLocalEvent]
    private void OnDidEquip(EntityUid uid, ModMagneticHarnessComponent harness, DidEquipEvent args)
    {
        if (_net.IsClient || _timing.ApplyingState || !harness.Slots.Contains(args.Slot)
            || !HasComp<GunComponent>(args.Equipment))
            return;

        var magnetized = EnsureComp<ModMagnetizedGunComponent>(args.Equipment);
        magnetized.Wearer = uid;
        magnetized.Slot = args.Slot;
        magnetized.LooseSince = null;
        magnetized.Launched = false;
        magnetized.Driving = false;
        magnetized.RetryAt = TimeSpan.Zero;
        Dirty(args.Equipment, magnetized);
        PhysicsSystem.UpdateIsPredicted(args.Equipment);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<ModMagnetizedGunComponent, PhysicsComponent>();
        while (query.MoveNext(out var gun, out var magnetized, out var body))
        {
            if (prediction && !body.Predict)
                continue;

            var owner = magnetized.Wearer;
            if (TerminatingOrDeleted(owner) || !TryComp(owner, out ModMagneticHarnessComponent? harness))
            {
                Reset((gun, magnetized), body);
                if (_net.IsServer)
                    _forget.Add(gun);
                continue;
            }

            if (_container.TryGetContainingContainer(gun, out var container))
            {
                Reset((gun, magnetized), body);
                if (_net.IsServer && container.Owner != owner)
                    _forget.Add(gun);
                continue;
            }

            if (Transform(gun).Anchored || _container.IsEntityInContainer(owner) || !_blocker.CanInteract(owner, null))
            {
                Reset((gun, magnetized), body);
                continue;
            }

            var gunPos = TransformSystem.GetMapCoordinates(gun);
            var ownerPos = TransformSystem.GetMapCoordinates(owner);
            var offset = ownerPos.Position - gunPos.Position;
            var distance = offset.Length();
            if (gunPos.MapId == MapId.Nullspace || gunPos.MapId != ownerPos.MapId || distance > harness.Range)
            {
                Reset((gun, magnetized), body);
                continue;
            }

            if (now < magnetized.RetryAt)
                continue;

            if (magnetized.LooseSince is not { } since)
            {
                magnetized.LooseSince = now;
                Dirty(gun, magnetized);
                PhysicsSystem.UpdateIsPredicted(gun);
                continue;
            }

            var elapsed = now - since;
            if (elapsed < harness.Delay)
                continue;

            if (distance <= harness.CatchDistance)
            {
                if (_net.IsServer)
                    _catch.Add(gun);
                continue;
            }

            if (elapsed > harness.Delay + harness.PullTime + harness.GiveUpTime)
            {
                Reset((gun, magnetized), body, now + RetryDelay);
                continue;
            }

            var launching = elapsed >= harness.Delay + harness.PullTime;
            if (launching && !magnetized.Launched)
            {
                magnetized.Launched = true;
                Dirty(gun, magnetized);
                _audio.PlayPredicted(harness.LaunchSound, gun, owner);
            }

            if (!magnetized.Driving)
            {
                magnetized.Driving = true;
                Dirty(gun, magnetized);
                PhysicsSystem.SetBodyStatus(gun, body, BodyStatus.InAir);
            }

            var speed = launching ? harness.LaunchSpeed : harness.PullSpeed;
            var xform = Transform(gun);
            var parentRotation = TransformSystem.GetWorldRotation(xform) - xform.LocalRotation;
            var velocity = (-parentRotation).RotateVec(offset / distance * speed);
            PhysicsSystem.SetLinearVelocity(gun, velocity, body: body);
            PhysicsSystem.SetAngularVelocity(gun, 0f, body: body);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var gun in _forget)
        {
            if (!TerminatingOrDeleted(gun))
                RemComp<ModMagnetizedGunComponent>(gun);
        }
        _forget.Clear();

        foreach (var gun in _catch)
        {
            if (TerminatingOrDeleted(gun) || !TryComp(gun, out ModMagnetizedGunComponent? magnetized)
                || !TryComp(gun, out PhysicsComponent? body) || _container.IsEntityInContainer(gun)
                || !TryComp(magnetized.Wearer, out ModMagneticHarnessComponent? harness))
                continue;

            if (_interaction.InRangeUnobstructed(magnetized.Wearer, gun, harness.CatchDistance)
                && TryCatch(gun, magnetized, magnetized.Wearer, harness))
                Reset((gun, magnetized), body);
            else
                Reset((gun, magnetized), body, _timing.CurTime + RetryDelay);
        }
        _catch.Clear();
    }

    private bool TryCatch(EntityUid gun, ModMagnetizedGunComponent magnetized, EntityUid owner, ModMagneticHarnessComponent harness)
    {
        var caught = _inventory.TryEquip(owner, gun, magnetized.Slot, silent: true);
        if (!caught)
        {
            foreach (var slot in harness.Slots)
            {
                if (slot != magnetized.Slot && _inventory.TryEquip(owner, gun, slot, silent: true))
                {
                    caught = true;
                    break;
                }
            }
        }

        if (!caught)
            caught = _hands.TryPickupAnyHand(owner, gun);

        if (caught)
            _audio.PlayPvs(harness.CatchSound, owner);

        return caught;
    }

    private void Reset(Entity<ModMagnetizedGunComponent> ent, PhysicsComponent body, TimeSpan? retryAt = null)
    {
        if (ent.Comp.Driving)
        {
            if (!_container.IsEntityInContainer(ent))
                PhysicsSystem.SetLinearVelocity(ent, Vector2.Zero, body: body);
            PhysicsSystem.SetBodyStatus(ent, body, BodyStatus.OnGround);
        }

        if (retryAt is { } time)
            ent.Comp.RetryAt = time;

        if (ent.Comp.LooseSince == null && !ent.Comp.Launched && !ent.Comp.Driving)
            return;

        ent.Comp.LooseSince = null;
        ent.Comp.Launched = false;
        ent.Comp.Driving = false;
        Dirty(ent);
        PhysicsSystem.UpdateIsPredicted(ent);
    }
}
