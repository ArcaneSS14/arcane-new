using System.Threading;
using System.Threading.Tasks;
using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PickNearbyFillableItemOperator : HTNOperator
{
    private FillbotSystem _bots = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _paths = default!;

    /// <summary>Search radius in map units, matching Goob's ten-tile pickup range.</summary>
    [DataField]
    public float Range = 10f;

    [DataField]
    public string TargetMoveKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<FillbotSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _paths = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_bots.TryGetSink(owner, out var sink))
            return (false, null);

        foreach (var item in _lookup.GetEntitiesInRange(owner, Range))
        {
            if (cancelToken.IsCancellationRequested)
                return (false, null);

            if (!_bots.CanCollect(owner, item, sink))
                continue;

            var path = await _paths.GetPath(owner, item,
                SharedInteractionSystem.InteractionRange - 1f, cancelToken, _paths.GetFlags(blackboard));

            if (cancelToken.IsCancellationRequested || !_bots.TryGetSink(owner, out var currentSink) ||
                currentSink != sink)
                return (false, null);

            if (path.Result != PathResult.Path || !_bots.CanCollect(owner, item, sink))
                continue;

            return (true, new()
            {
                { FillbotBlackboard.Item, item },
                { FillbotBlackboard.Receiver, sink },
                { TargetMoveKey, new EntityCoordinates(item, Vector2.Zero) },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        blackboard.Remove<EntityUid>(FillbotBlackboard.Item);
        blackboard.Remove<EntityUid>(FillbotBlackboard.Receiver);
    }
}
