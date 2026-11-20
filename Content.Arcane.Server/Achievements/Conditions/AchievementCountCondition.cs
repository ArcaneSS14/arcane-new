using Content.Arcane.Shared.Achievements;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class AchievementCountCondition : AchievementCondition
{
    [DataField]
    public ProtoId<AchievementCategoryPrototype>? Category;
}

public sealed partial class AchievementCountConditionSystem : AchievementConditionSystem<AchievementCountCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        Achievements.SessionLoaded += CheckSession;
        Achievements.AchievementUnlocked += OnUnlocked;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        Achievements.SessionLoaded -= CheckSession;
        Achievements.AchievementUnlocked -= OnUnlocked;
    }

    private void OnUnlocked(ICommonSession session, AchievementPrototype unlocked)
    {
        CheckSession(session);
    }

    private void CheckSession(ICommonSession session)
    {
        foreach (var (proto, condition) in Entries)
        {
            Achievements.SetDerivedProgress(session, proto, Achievements.CountUnlocked(session, condition.Category));
        }
    }
}
