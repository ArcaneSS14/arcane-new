using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PlantbotServiceOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private PlantbotServiceSystem _bots = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<PlantbotServiceSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entities) &&
               _bots.TryService(owner, target)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
        => blackboard.Remove<EntityUid>(TargetKey);
}
