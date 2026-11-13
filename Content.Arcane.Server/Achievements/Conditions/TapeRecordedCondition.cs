using Content.Goobstation.Server.TapeRecorder;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class TapeRecordedCondition : AchievementCondition;

public sealed partial class TapeRecordedConditionSystem : AchievementConditionSystem<TapeRecordedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, TapeRecordedEvent>(OnRecorded);
    }

    private void OnRecorded(Entity<ActorComponent> ent, ref TapeRecordedEvent args)
    {
        ProgressAll(ent);
    }
}
