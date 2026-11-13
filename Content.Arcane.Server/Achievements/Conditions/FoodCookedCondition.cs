using Content.Trauma.Common.Kitchen;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class FoodCookedCondition : AchievementCondition;

public sealed partial class FoodCookedConditionSystem : AchievementConditionSystem<FoodCookedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, CookedFoodEvent>(OnCooked);
    }

    private void OnCooked(Entity<ActorComponent> ent, ref CookedFoodEvent args)
    {
        if (args.Count > 0)
            ProgressAll(ent, args.Count);
    }
}
