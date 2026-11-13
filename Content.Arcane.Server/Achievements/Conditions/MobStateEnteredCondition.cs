using Content.Server.Hands.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class MobStateEnteredCondition : AchievementCondition
{
    [DataField]
    public MobState MobState = MobState.Dead;
}

public sealed partial class MobStateEnteredConditionSystem : AchievementConditionSystem<MobStateEnteredCondition>
{
    private readonly Dictionary<EntityUid, MobState> _handled = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, DropHandItemsEvent>(OnDropHandItems, before: [typeof(HandsSystem)]);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobStateComponent, ComponentRemove>(OnMobRemoved);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnDropHandItems(Entity<MobStateComponent> ent, ref DropHandItemsEvent args)
    {
        var state = ent.Comp.CurrentState;
        if (state is not (MobState.Critical or MobState.Dead)
            || _handled.TryGetValue(ent, out var handled) && handled == state)
            return;

        _handled[ent] = state;

        foreach (var (proto, condition) in Entries)
        {
            if (condition.MobState == state)
                Achievements.AddProgress(ent, proto);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        // MobStateSystem raises DropHandItems before MobStateChanged. Retain that entry's guard.
        if (_handled.TryGetValue(args.Target, out var handled) && handled != args.NewMobState)
            _handled.Remove(args.Target);
    }

    private void OnMobRemoved(Entity<MobStateComponent> ent, ref ComponentRemove args)
    {
        _handled.Remove(ent.Owner);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _handled.Clear();
    }
}
