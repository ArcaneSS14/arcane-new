using Content.Goobstation.Server.Supermatter.Systems;
using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.Surgery;
using Content.Server.Access.Systems;
using Content.Server.Nuke;
using Content.Shared.Body;
using Content.Shared.Chasm;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class MicrowaveIdCondition : AchievementCondition;
public sealed partial class SupermatterConsumedCondition : AchievementCondition;
public sealed partial class AbductedAgainCondition : AchievementCondition;
public sealed partial class NukeDisarmCondition : AchievementCondition;
public sealed partial class ChasmFallCondition : AchievementCondition;

public sealed partial class MicrowaveIdConditionSystem : AchievementConditionSystem<MicrowaveIdCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, MicrowaveIdAccessChangedEvent>(OnChanged);
    }

    private void OnChanged(Entity<MetaDataComponent> ent, ref MicrowaveIdAccessChangedEvent args) => ProgressAll(ent);
}

public sealed partial class SupermatterConsumedConditionSystem : AchievementConditionSystem<SupermatterConsumedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, SupermatterEntityConsumedEvent>(OnConsumed);
    }

    private void OnConsumed(Entity<ActorComponent> ent, ref SupermatterEntityConsumedEvent args) => ProgressAll(ent);
}

public sealed partial class AbductedAgainConditionSystem : AchievementConditionSystem<AbductedAgainCondition>
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, SurgeryOrganAddedEvent>(OnSurgery);
    }

    private void OnSurgery(Entity<ActorComponent> ent, ref SurgeryOrganAddedEvent args)
    {
        if (!HasComp<AbductorComponent>(args.User)
            || HasComp<AbductorComponent>(ent)
            || !HasComp<AbductorOrganComponent>(args.Organ)
            || !TryComp<AbductorVictimComponent>(ent, out var victim)
            || !victim.Implanted
            || _body.GetBody(args.Organ) != ent.Owner)
            return;

        ProgressAll(ent);
    }
}

public sealed partial class NukeDisarmConditionSystem : AchievementConditionSystem<NukeDisarmCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, NukeDisarmedByUserEvent>(OnDisarmed);
    }

    private void OnDisarmed(Entity<ActorComponent> ent, ref NukeDisarmedByUserEvent args) => ProgressAll(ent);
}

public sealed partial class ChasmFallConditionSystem : AchievementConditionSystem<ChasmFallCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChasmFallingComponent, StartedFallingIntoChasmEvent>(OnFalling);
    }

    private void OnFalling(Entity<ChasmFallingComponent> ent, ref StartedFallingIntoChasmEvent args) => ProgressAll(ent);
}
