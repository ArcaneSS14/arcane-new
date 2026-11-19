using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Arcane.Server.Xenobiology;

public sealed partial class PickSlimeCorpseOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private NpcFactionSystem _factions = default!;
    private PathfindingSystem _pathfinding = default!;
    private SatiationSystem _satiation = default!;
    private SlimeCorpseEaterSystem _eater = default!;
    private static readonly SatiationValue Peckish = "Peckish";

    [DataField(required: true)]
    public string RangeKey = string.Empty;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField(required: true)]
    public string CorpseKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _factions = sysManager.GetEntitySystem<NpcFactionSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _satiation = sysManager.GetEntitySystem<SatiationSystem>();
        _eater = sysManager.GetEntitySystem<SlimeCorpseEaterSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entities)
            || !_entities.TryGetComponent<SatiationComponent>(owner, out var satiation)
            || _satiation.IsValueInRange((owner, satiation), SatiationSystem.Hunger, above: Peckish))
            return (false, null);

        foreach (var target in _factions.GetNearbyHostiles(owner, range))
        {
            if (!_eater.CanTargetCorpse(owner, target) || !_eater.HasEdibleOrgans(target))
                continue;

            var path = await _pathfinding.GetPath(owner, target, 1f, cancelToken);
            if (path.Result != PathResult.Path || !_entities.TryGetComponent<TransformComponent>(target, out var xform))
                continue;

            return (true, new Dictionary<string, object>
            {
                [TargetKey] = xform.Coordinates,
                [CorpseKey] = target,
                [NPCBlackboard.PathfindKey] = path,
            });
        }

        return (false, null);
    }
}
