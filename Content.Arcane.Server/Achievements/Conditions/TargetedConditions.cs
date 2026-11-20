using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Polymorph;
using Content.Shared.Kitchen;
using Content.Server.Holiday.Christmas;
using Content.Server.GameTicking;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Arcane.Common.Explosion;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public abstract partial class PrototypeFilteredCondition : AchievementCondition
{
    [DataField]
    public List<EntProtoId>? Prototypes;
}

public sealed partial class ButcherCondition : PrototypeFilteredCondition;
public sealed partial class EatenCondition : PrototypeFilteredCondition;
public sealed partial class PolymorphedIntoCondition : PrototypeFilteredCondition;
public sealed partial class GiftOpenedCondition : PrototypeFilteredCondition;

public sealed partial class ArtifactExplosionCondition : AchievementCondition
{
    [DataField]
    public TimeSpan Within = TimeSpan.FromMinutes(7);
}

public abstract partial class PrototypeFilteredConditionSystem<T> : AchievementConditionSystem<T>
    where T : PrototypeFilteredCondition
{
    protected void ProgressMatching(EntityUid player, EntityUid target)
    {
        var prototype = Prototype(target);
        foreach (var (achievement, condition) in Entries)
        {
            if (condition.Prototypes is { } allowed && (prototype == null || !allowed.Contains(prototype.ID)))
                continue;

            Achievements.AddProgress(player, achievement);
        }
    }
}

public sealed partial class ButcherConditionSystem : PrototypeFilteredConditionSystem<ButcherCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, KitchenSpikeButcheredEvent>(OnButchered);
    }

    private void OnButchered(Entity<MobStateComponent> ent, ref KitchenSpikeButcheredEvent args)
    {
        ProgressMatching(args.User, ent);
    }
}

public sealed partial class EatenConditionSystem : PrototypeFilteredConditionSystem<EatenCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, FullyEatenEvent>(OnEaten);
    }

    private void OnEaten(Entity<MobStateComponent> ent, ref FullyEatenEvent args)
    {
        ProgressMatching(args.User, ent);
    }
}

public sealed partial class PolymorphedIntoConditionSystem : PrototypeFilteredConditionSystem<PolymorphedIntoCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, PolymorphedEvent>(OnPolymorphed);
    }

    private void OnPolymorphed(Entity<ActorComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert && ent.Owner == args.NewEntity)
            ProgressMatching(ent, ent);
    }
}

public sealed partial class GiftOpenedConditionSystem : PrototypeFilteredConditionSystem<GiftOpenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, RandomGiftOpenedEvent>(OnOpened);
    }

    private void OnOpened(Entity<MetaDataComponent> ent, ref RandomGiftOpenedEvent args)
    {
        ProgressMatching(ent, args.Contents);
    }
}

public sealed partial class ArtifactExplosionConditionSystem : AchievementConditionSystem<ArtifactExplosionCondition>
{
    [Dependency] private ServerGameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XAETriggerExplosivesComponent, ExplosiveTriggeredEvent>(OnTriggered);
    }

    private void OnTriggered(Entity<XAETriggerExplosivesComponent> ent, ref ExplosiveTriggeredEvent args)
    {
        if (args.User is not { } user)
            return;
        var elapsed = _ticker.RoundDuration();
        foreach (var (proto, condition) in Entries)
        {
            if (elapsed <= condition.Within)
                Achievements.AddProgress(user, proto);
        }
    }
}