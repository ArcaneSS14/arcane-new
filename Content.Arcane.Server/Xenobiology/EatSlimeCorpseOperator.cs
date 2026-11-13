using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.DoAfter;

namespace Content.Arcane.Server.Xenobiology;

public sealed partial class EatSlimeCorpseOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private SharedDoAfterSystem _doAfter = default!;
    private SlimeCorpseEaterSystem _eater = default!;

    [DataField(required: true)]
    public string CorpseKey = string.Empty;

    private const string DoAfterKey = "SlimeCorpseDoAfter";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _doAfter = sysManager.GetEntitySystem<SharedDoAfterSystem>();
        _eater = sysManager.GetEntitySystem<SlimeCorpseEaterSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (blackboard.TryGetValue<EntityUid>(CorpseKey, out var corpse, _entities)
            && _eater.TryStartEating(owner, corpse, out var id) && id is { } active)
            blackboard.SetValue(DoAfterKey, active);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<DoAfterId>(DoAfterKey, out var id, _entities))
            return HTNOperatorStatus.Failed;

        return _doAfter.GetStatus(id) switch
        {
            DoAfterStatus.Running => HTNOperatorStatus.Continuing,
            DoAfterStatus.Finished => HTNOperatorStatus.Finished,
            _ => HTNOperatorStatus.Failed,
        };
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        if (blackboard.TryGetValue<DoAfterId>(DoAfterKey, out var id, _entities) && _doAfter.IsRunning(id))
            _doAfter.Cancel(id);

        blackboard.Remove<DoAfterId>(DoAfterKey);
    }
}
