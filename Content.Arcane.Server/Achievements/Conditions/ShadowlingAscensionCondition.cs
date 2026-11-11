using Content.Goobstation.Shared.Shadowling;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class ShadowlingAscensionCondition : AchievementCondition;


public sealed partial class ShadowlingAscensionConditionSystem : AchievementConditionSystem<ShadowlingAscensionCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingAscendEvent>(OnAscend);
    }

    private void OnAscend(ShadowlingAscendEvent ev)
    {
        ProgressAll(ev.ShadowlingAscended);
    }
}
