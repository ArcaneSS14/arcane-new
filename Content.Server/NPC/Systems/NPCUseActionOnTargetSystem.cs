using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Robust.Shared.Timing;
using Robust.Shared.Random; // Arcane

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IRobustRandom _random = default!; // Arcane

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);

        // Arcane-Start
        foreach (var actionId in ent.Comp.ExtraActionIds)
        {
            if (_actions.AddAction(ent, actionId) is { } extra)
                ent.Comp.ExtraActionEnts.Add(extra);
        }
        // Arcane-End
    }

    public bool TryUseTentacleAttack(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        // Arcane-Edit-Start: pick one of the main and extra actions
        var actionEnt = user.Comp.ActionEnt;
        if (user.Comp.ExtraActionEnts.Count > 0)
        {
            var index = _random.Next(user.Comp.ExtraActionEnts.Count + 1);
            if (index > 0)
                actionEnt = user.Comp.ExtraActionEnts[index - 1];
        }

        if (_actions.GetAction(actionEnt) is not {} action)
            return false;
        // Arcane-Edit-End

        if (!_actions.ValidAction(action))
            return false;

        _actions.SetEventTarget(action, target);

        // NPC is serverside, no prediction :(
        _actions.PerformAction(user.Owner, action, predicted: false);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            TryUseTentacleAttack((uid, comp), target);
        }
    }
}
