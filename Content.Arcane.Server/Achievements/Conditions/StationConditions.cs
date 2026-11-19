using Content.Goobstation.Shared.GameTicking.Rules;
using Content.Goobstation.Server.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking;
using Content.Server.Nuke;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Singularity.Events;
using Content.Server.Tesla.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Projectiles;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Singularity.Components;
using Content.Shared.Station.Systems;
using Content.Shared.Zombies;
using Content.Trauma.Shared.Wizard.TimeStop;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class NukiePaybackCondition : AchievementCondition;

public sealed partial class ResearchCompleteCondition : AchievementCondition;

public sealed partial class BlobDefeatedCondition : AchievementCondition;

public sealed partial class ZombieVictoryCondition : AchievementCondition;

public sealed partial class SingularityBustedCondition : AchievementCondition;

public sealed partial class ChronofieldCondition : AchievementCondition
{
    [DataField]
    public float Radius = 3f;
}

public abstract partial class SessionsConditionSystem<T> : AchievementConditionSystem<T> where T : AchievementCondition
{
    [Dependency] protected IPlayerManager PlayerManager = default!;

    protected void ProgressAllPlayers()
    {
        foreach (var session in PlayerManager.Sessions)
        {
            if (session.AttachedEntity is { } player)
                ProgressAll(player);
        }
    }
}

public sealed partial class NukiePaybackConditionSystem : SessionsConditionSystem<NukiePaybackCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeopsOutpostDestroyedEvent>(OnOutpostDestroyed);
    }

    private void OnOutpostDestroyed(NukeopsOutpostDestroyedEvent ev)
    {
        ProgressAllPlayers();
    }
}

public sealed partial class ResearchCompleteConditionSystem : SessionsConditionSystem<ResearchCompleteCondition>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StationSystem _station = default!;

    private readonly HashSet<EntityUid> _completed = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDatabaseComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnDatabaseModified(Entity<TechnologyDatabaseComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        var disciplines = ent.Comp.SupportedDisciplines;
        var unlocked = ent.Comp.ResearchedTechnologies;
        if (Entries.Count == 0 || disciplines.Count == 0 || _completed.Contains(ent))
            return;

        foreach (var tech in _proto.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Hidden || !disciplines.Contains(tech.Discipline))
                continue;

            if (!unlocked.Contains(tech.ID))
                return;
        }

        _completed.Add(ent);

        var station = _station.GetOwningStation(ent);
        if (station == null)
            return;

        foreach (var session in PlayerManager.Sessions)
        {
            if (session.AttachedEntity is { } player && _station.GetOwningStation(player) == station)
                ProgressAll(player);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _completed.Clear();
    }
}

public sealed partial class BlobDefeatedConditionSystem : SessionsConditionSystem<BlobDefeatedCondition>
{
    [Dependency] private ServerGameTicker _ticker = default!;

    private bool _nukeDetonated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, BlobCoreShutdownEvent>(OnCoreShutdown);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnCoreShutdown(Entity<BlobCoreComponent> ent, ref BlobCoreShutdownEvent args)
    {
        if (Entries.Count == 0 || _nukeDetonated || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        var cores = EntityQueryEnumerator<BlobCoreComponent>();
        while (cores.MoveNext(out var core, out _))
        {
            if (core != ent.Owner && !TerminatingOrDeleted(core))
                return;
        }

        var critical = false;
        var rules = EntityQueryEnumerator<ActiveGameRuleComponent, BlobRuleComponent>();
        while (rules.MoveNext(out _, out _, out var rule))
        {
            if (rule.Stage == BlobStage.Critical)
                critical = true;
        }

        if (critical)
            ProgressAllPlayers();
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        _nukeDetonated = true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _nukeDetonated = false;
    }
}

public sealed partial class ZombieVictoryConditionSystem : SessionsConditionSystem<ZombieVictoryCondition>
{
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(ref RoundEndTextAppendEvent ev)
    {
        if (Entries.Count == 0)
            return;

        var zombies = 0;
        foreach (var session in PlayerManager.Sessions)
        {
            if (session.AttachedEntity is not { } player
                || !HasComp<HumanoidProfileComponent>(player)
                || !_mobState.IsAlive(player))
                continue;

            if (!HasComp<ZombieComponent>(player))
                return;

            zombies++;
        }

        if (zombies > 0)
            ProgressAllPlayers();
    }
}

public sealed partial class SingularityBustedConditionSystem : AchievementConditionSystem<SingularityBustedCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, EventHorizonConsumedEntityEvent>(OnConsumed, after: [typeof(SingularitySystem)]);
    }

    private void OnConsumed(Entity<ProjectileComponent> ent, ref EventHorizonConsumedEntityEvent args)
    {
        if (ent.Comp.Shooter is not { } shooter)
            return;

        var dead = TryComp<SingularityComponent>(args.EventHorizonUid, out var singulo) && singulo.Energy <= 0
                   || TryComp<TeslaEnergyBallComponent>(args.EventHorizonUid, out var tesla) && tesla.Energy < tesla.EnergyToDespawn;

        if (dead)
            ProgressAll(shooter);
    }
}

public sealed partial class ChronofieldConditionSystem : AchievementConditionSystem<ChronofieldCondition>
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FreezeContactsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<FreezeContactsComponent> ent, ref MapInitEvent args)
    {
        var center = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (proto, condition) in Entries)
        {
            foreach (var session in _player.Sessions)
            {
                if (session.AttachedEntity is not { } player)
                    continue;

                var position = _transform.GetMapCoordinates(player);
                if (position.MapId == center.MapId && (position.Position - center.Position).Length() <= condition.Radius)
                    Achievements.AddProgress(player, proto);
            }
        }
    }
}
