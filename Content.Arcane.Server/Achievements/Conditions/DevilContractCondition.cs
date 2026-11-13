using Content.Goobstation.Shared.Devil.Contract;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class DevilContractCondition : AchievementCondition;

public sealed partial class DevilContractConditionSystem : AchievementConditionSystem<DevilContractCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, DevilContractSignedEvent>(OnSigned);
    }

    private void OnSigned(Entity<ActorComponent> ent, ref DevilContractSignedEvent args)
    {
        ProgressAll(ent);
    }
}
