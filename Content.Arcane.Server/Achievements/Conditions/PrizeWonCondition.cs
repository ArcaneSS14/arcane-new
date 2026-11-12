using Content.Arcane.Shared.Gambling;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class PrizeWonCondition : AchievementCondition
{
    [DataField(required: true)]
    public List<ProtoId<PrizePrototype>> Prizes = new();
}

public sealed partial class PrizeWonConditionSystem : AchievementConditionSystem<PrizeWonCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, PrizeWonEvent>(OnWon);
    }

    private void OnWon(Entity<ActorComponent> ent, ref PrizeWonEvent args)
    {
        foreach (var (prototype, condition) in Entries)
        {
            if (condition.Prizes.Contains(args.Prize))
                Achievements.AddProgress(ent, prototype);
        }
    }
}
