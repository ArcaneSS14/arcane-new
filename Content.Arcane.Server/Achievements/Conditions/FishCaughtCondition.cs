using Content.Goobstation.Shared.Fishing.Events;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class FishCaughtCondition : AchievementCondition;

public sealed partial class FishCaughtConditionSystem : AchievementConditionSystem<FishCaughtCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, FishCaughtEvent>(OnFishCaught);
    }

    private void OnFishCaught(Entity<MetaDataComponent> ent, ref FishCaughtEvent args)
    {
        ProgressAll(args.User);
    }
}
