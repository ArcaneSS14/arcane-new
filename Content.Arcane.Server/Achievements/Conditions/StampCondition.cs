using Content.Shared.Paper;
using Content.Shared.Item;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class StampCondition : AchievementCondition;

public sealed partial class StampConditionSystem : AchievementConditionSystem<StampCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemComponent, PaperStampAppliedEvent>(OnStamped);
    }

    private void OnStamped(Entity<ItemComponent> ent, ref PaperStampAppliedEvent args)
    {
        if (args.User is { } user && HasComp<PaperComponent>(ent))
            ProgressAll(user);
    }
}
