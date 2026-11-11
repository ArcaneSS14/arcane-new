using Content.Shared.Body;
using Content.Shared.Gibbing;
using Content.Shared.Mind.Components;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class GibbedCondition : AchievementCondition;

public sealed partial class GibbedConditionSystem : AchievementConditionSystem<GibbedCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, BeingGibbedEvent>(OnGibbed, before: [typeof(BodySystem)]);
    }

    private void OnGibbed(Entity<MindContainerComponent> ent, ref BeingGibbedEvent args)
    {
        if (HasComp<BodyComponent>(ent))
            ProgressAll(ent);
    }
}
