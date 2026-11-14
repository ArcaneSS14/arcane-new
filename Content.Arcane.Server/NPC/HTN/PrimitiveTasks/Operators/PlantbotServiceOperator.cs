using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PlantbotServiceOperator : HTNOperator
{
    internal const string BlockedTraysKey = "PlantbotBlockedTrays";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IGameTiming _timing = default!;
    private SharedInteractionSystem _interaction = default!;
    private PlantbotServiceSystem _bots = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _bots = sysManager.GetEntitySystem<PlantbotServiceSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entities))
            return HTNOperatorStatus.Failed;

        if (_bots.TryService(owner, target))
            return HTNOperatorStatus.Finished;

        if (_bots.CanService(owner, target) && !_interaction.InRangeAndAccessible(owner, target))
        {
            var now = _timing.CurTime;
            // Planning shallow-copies the blackboard, so replace the snapshot instead of mutating it.
            var blocked = new Dictionary<EntityUid, TimeSpan>();
            if (blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(BlockedTraysKey, out var previous, _entities))
            {
                foreach (var (tray, retryAt) in previous)
                {
                    if (retryAt > now)
                        blocked.Add(tray, retryAt);
                }
            }

            blocked[target] = now + RetryDelay;
            blackboard.SetValue(BlockedTraysKey, blocked);
        }

        return HTNOperatorStatus.Failed;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
        => blackboard.Remove<EntityUid>(TargetKey);
}
