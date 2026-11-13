using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// A successful generic interaction need not mean an item was picked up.
/// Only advance the delivery plan once the selected item is actually held.
/// </summary>
public sealed partial class PickupFillableItemOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private FillbotSystem _bots = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<FillbotSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityUid>(FillbotBlackboard.Item, out var item, _entities) ||
            !blackboard.TryGetValue<EntityUid>(FillbotBlackboard.Receiver, out var sink, _entities) ||
            !_bots.TryPickup(owner, item, sink))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
