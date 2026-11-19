using System.Threading;
using System.Threading.Tasks;
using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PickNearbyServicableHydroponicsTrayOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IGameTiming _timing = default!;

    private PlantbotServiceSystem _bots = default!;
    private SharedInteractionSystem _interaction = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _paths = default!;

    [DataField]
    public float Range = 4f;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetMoveKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<PlantbotServiceSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _paths = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(PlantbotServiceOperator.BlockedTraysKey,
            out var blocked, _entities);

        foreach (var target in _lookup.GetEntitiesInRange(owner, Range))
        {
            if (cancelToken.IsCancellationRequested)
                return (false, null);

            if ((blocked != null && blocked.TryGetValue(target, out var retryAt) && retryAt > _timing.CurTime) ||
                !_bots.CanService(owner, target))
                continue;

            var effects = new Dictionary<string, object>
            {
                { TargetKey, target },
                { TargetMoveKey, new EntityCoordinates(target, Vector2.Zero) },
            };
            if (_interaction.InRangeAndAccessible(owner, target))
                return (true, effects);

            var path = await _paths.GetPath(owner, target,
                SharedInteractionSystem.InteractionRange - 1f, cancelToken, _paths.GetFlags(blackboard));

            if (cancelToken.IsCancellationRequested)
                return (false, null);

            if (path.Result != PathResult.Path || !_bots.CanService(owner, target))
                continue;

            effects.Add(NPCBlackboard.PathfindKey, path);
            return (true, effects);
        }

        return (false, null);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
        => blackboard.Remove<EntityUid>(TargetKey);
}
