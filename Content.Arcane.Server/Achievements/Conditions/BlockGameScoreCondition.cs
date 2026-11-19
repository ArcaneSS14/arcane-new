using Content.Server.Arcade.BlockGame;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class BlockGameScoreCondition : AchievementCondition
{
    [DataField(required: true)]
    public int Score;
}

public sealed partial class BlockGameScoreConditionSystem : AchievementConditionSystem<BlockGameScoreCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, BlockGameOverEvent>(OnGameOver);
    }

    private void OnGameOver(Entity<ActorComponent> ent, ref BlockGameOverEvent args)
    {
        foreach (var (proto, condition) in Entries)
        {
            if (args.Points >= condition.Score)
                Achievements.AddProgress(ent, proto);
        }
    }
}
