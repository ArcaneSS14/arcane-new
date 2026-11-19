using System.Threading;
using System.Threading.Tasks;
using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class FindLinkedMachineOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private FillbotSystem _bots = default!;
    private SharedHandsSystem _hands = default!;
    private PathfindingSystem _paths = default!;

    /// <summary>Delivery-only branch for an item already held after interruption.</summary>
    [DataField]
    public bool RequireHeldItem;

    [DataField]
    public string TargetMoveKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<FillbotSystem>();
        _hands = sysManager.GetEntitySystem<SharedHandsSystem>();
        _paths = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_bots.TryGetSink(owner, out var sink))
            return (false, null);

        EntityUid item;
        if (RequireHeldItem)
        {
            if (_hands.GetActiveItem(owner) is not { } held)
                return (false, null);

            item = held;
        }
        else if (!blackboard.TryGetValue(FillbotBlackboard.Item, out item, _entities) ||
                 !blackboard.TryGetValue<EntityUid>(FillbotBlackboard.Receiver, out var plannedSink, _entities) ||
                 plannedSink != sink)
        {
            return (false, null);
        }

        if (!_bots.IsUsable(item) || _bots.IsBlocked(owner, item, sink))
            return (false, null);

        var coordinates = new EntityCoordinates(sink, Vector2.Zero);
        var start = blackboard.GetValueOrDefault<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, _entities);
        var path = await _paths.GetPath(owner, start, coordinates,
            SharedInteractionSystem.InteractionRange - 0.5f, cancelToken, _paths.GetFlags(blackboard));

        if (cancelToken.IsCancellationRequested || path.Result != PathResult.Path ||
            !_bots.IsUsable(item) || !_bots.TryGetSink(owner, out var currentSink) || currentSink != sink ||
            (RequireHeldItem && _hands.GetActiveItem(owner) != item))
            return (false, null);

        return (true, new()
        {
            { FillbotBlackboard.Item, item },
            { FillbotBlackboard.Receiver, sink },
            { TargetMoveKey, coordinates },
            { NPCBlackboard.PathfindKey, path },
        });
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        blackboard.Remove<EntityUid>(FillbotBlackboard.Item);
        blackboard.Remove<EntityUid>(FillbotBlackboard.Receiver);
    }
}
