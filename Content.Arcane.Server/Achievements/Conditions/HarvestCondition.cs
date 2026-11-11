using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class HarvestCondition : AchievementCondition;

public sealed partial class HarvestConditionSystem : AchievementConditionSystem<HarvestCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantHolderComponent, PlantHarvestedEvent>(OnHarvested);
    }

    private void OnHarvested(Entity<PlantHolderComponent> ent, ref PlantHarvestedEvent args)
    {
        ProgressAll(args.User);
    }
}
