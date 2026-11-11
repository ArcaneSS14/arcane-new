using Content.Arcane.Shared.Achievements;
using Content.Server.Mobs;
using Content.Server.Shuttles.Systems;
using Content.Server.Suicide;
using Content.Shared.Body;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class SuicideCondition : AchievementCondition;
public sealed partial class SuccumbCondition : AchievementCondition;
public sealed partial class ShuttleCrushedCondition : AchievementCondition;

public sealed partial class SuicideConditionSystem : AchievementConditionSystem<SuicideCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, SuicideCompletedEvent>(OnCompleted);
    }

    private void OnCompleted(Entity<MetaDataComponent> ent, ref SuicideCompletedEvent args)
    {
        if (HasComp<NoAchievementsComponent>(ent))
            return;
        foreach (var (proto, _) in Entries)
            Achievements.AddProgress(args.Session, proto);
    }
}

public sealed partial class SuccumbConditionSystem : AchievementConditionSystem<SuccumbCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, CritSuccumbedEvent>(OnCompleted);
    }

    private void OnCompleted(Entity<MetaDataComponent> ent, ref CritSuccumbedEvent args)
    {
        if (HasComp<NoAchievementsComponent>(ent))
            return;
        foreach (var (proto, _) in Entries)
            Achievements.AddProgress(args.Session, proto);
    }
}

public sealed partial class ShuttleCrushedConditionSystem : AchievementConditionSystem<ShuttleCrushedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, ShuttleCrushedEvent>(OnCrushed);
    }

    private void OnCrushed(Entity<BodyComponent> ent, ref ShuttleCrushedEvent args) => ProgressAll(ent);
}
