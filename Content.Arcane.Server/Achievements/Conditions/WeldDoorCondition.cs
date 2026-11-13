using Content.Shared.Doors.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Tools.Components;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class WeldDoorCondition : AchievementCondition;

public sealed partial class WeldDoorConditionSystem : AchievementConditionSystem<WeldDoorCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, WeldableChangedEvent>(OnWeldChanged);
    }

    private void OnWeldChanged(Entity<WeldableComponent> ent, ref WeldableChangedEvent args)
    {
        if (args.IsWelded && args.User is { } user && HasComp<DoorComponent>(ent))
            ProgressAll(user);
    }
}
