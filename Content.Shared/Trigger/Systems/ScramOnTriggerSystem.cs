using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Containers; // Arcane

namespace Content.Shared.Trigger.Systems;

public sealed partial class ScramOnTriggerSystem : XOnTriggerSystem<ScramOnTriggerComponent>
{
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private TurfSystem _turfSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!; // Arcane

    protected override void OnTrigger(Entity<ScramOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // Arcane-Start: only commit side effects after finding a valid destination.
        if (_net.IsClient || TerminatingOrDeleted(target) || Paused(target))
            return;

        if (SelectRandomTileInRange(target, ent.Comp.TeleportRadius) is not { } destination)
            return;
        // Arcane-End

        // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
        // This can for example happen when the user is cuffed and being pulled.
        if (TryComp<PullableComponent>(target, out var pull) && _pulling.IsPulled(target, pull))
            _pulling.TryStopPull(target, pull);

        // Check if the user is pulling anything, and drop it if so.
        if (TryComp<PullerComponent>(target, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        // Arcane-Start: teleport out of the container using the chosen grid coordinates.
        _audio.PlayPvs(ent.Comp.TeleportSound, target);
        _transform.SetCoordinates(target, destination);
        args.Handled = true;
        // Arcane-End
    }
    /// <summary>
    /// Method to find a random empty tile within a certain radius. Will not select off-grid tiles. Returns
    /// null if no tile is found within a certain number of tries.
    /// </summary>
    /// <remarks> Trends towards the outer radius. Compensates for small grids. </remarks>
    private EntityCoordinates? SelectRandomTileInRange(EntityUid uid, Vector2 radius, int tries = 40, PhysicsComponent? physicsComponent = null)
    {
        // Arcane-Start: nested container offsets are relative to the container, not the grid.
        var origin = uid;
        var xform = Transform(uid);
        if (_container.IsEntityOrParentInContainer(uid, xform: xform))
        {
            if (!_container.TryGetOuterContainer(uid, xform, out var container))
                return null;

            origin = container.Owner;
        }

        if (!_transform.TryGetMapOrGridCoordinates(origin, out var userCoords))
            return null;
        // Arcane-End
        EntityCoordinates? targetCoords = null;

        if (!Resolve(uid, ref physicsComponent))
            return targetCoords;


        for (var i = 0; i < tries; i++)
        {
            // distance = r * sq(x) * i
            // r = the radius of the search area.
            // sq(x) = the square root of [0 - 1]. Gives a number trending to the
            // upper range of [0, 1] so that you tend to teleport further.
            // i = A percentage based on the current try count, which results in each
            // subsequent try landing closer and closer towards the entity.
            // Beneficial for smaller maps, especially when the radius is large.
            var distance = (radius.Y - radius.X) * MathF.Sqrt(_random.NextFloat()) * (1 - (float)i / tries) + radius.X;

            // We then offset the user coords from a random angle * distance
            var tempTargetCoords = userCoords.Value.Offset(_random.NextAngle().ToVec() * distance);

            if (!_turfSystem.TryGetTileRef(tempTargetCoords, out var tileRef)
                || _turfSystem.IsSpace(tileRef.Value)
                || _turfSystem.IsTileBlocked(tileRef.Value, (CollisionGroup)physicsComponent.CollisionMask))
                continue;

            targetCoords = tempTargetCoords;
            break;
        }

        return targetCoords;
    }
}
