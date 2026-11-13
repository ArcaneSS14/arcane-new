using Content.Shared.Mobs.Systems;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Lavaland.Server.Mobs;

public sealed partial class LavalandMobSpawnerSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    private const float CheckInterval = 0.25f;
    private readonly HashSet<Entity<ActorComponent>> _nearbyActors = new();
    private float _timeUntilCheck = CheckInterval;

    public override void Update(float frameTime)
    {
        _timeUntilCheck -= frameTime;
        if (_timeUntilCheck > 0f)
            return;

        _timeUntilCheck = CheckInterval;
        var query = EntityQueryEnumerator<LavalandMobSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var marker, out var transform))
        {
            if (TryGetNearbyPlayer(transform.Coordinates, marker.ActivationRange, out var player))
                _trigger.Trigger(uid, player, predicted: false);
        }
    }

    internal bool TryGetNearbyPlayer(EntityCoordinates coordinates, float range, out EntityUid player)
    {
        _nearbyActors.Clear();
        _lookup.GetEntitiesInRange(coordinates, range, _nearbyActors, LookupFlags.Dynamic);
        foreach (var actor in _nearbyActors)
        {
            if (!_mobState.IsAlive(actor.Owner))
                continue;

            player = actor.Owner;
            return true;
        }

        player = EntityUid.Invalid;
        return false;
    }
}
