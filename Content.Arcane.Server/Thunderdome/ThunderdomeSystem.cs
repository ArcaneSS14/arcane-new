using Content.Arcane.Common.Thunderdome;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Spawners.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Thunderdome;

public sealed partial class ThunderdomeSystem : GameRuleSystem<ThunderdomeRuleComponent>
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId RulePrototype = "ThunderdomeRule";
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan CleanupDelay = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThunderdomeRuleComponent, RuleLoadedGridsEvent>(OnGridsLoaded);
        SubscribeLocalEvent<ThunderdomeProtectedComponent, BeforeDamageChangedEvent>(OnArenaDamage);
        InitializePlayers();
        InitializeUi();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => CloseLoadoutMenus());
    }

    private void OnGridsLoaded(Entity<ThunderdomeRuleComponent> rule, ref RuleLoadedGridsEvent args)
    {
        rule.Comp.Map = args.Map;
        rule.Comp.Grids.UnionWith(args.Grids);
        EnsureComp<ThunderdomeMapComponent>(_maps.GetMapOrInvalid(args.Map));

        // Protect the arena fixtures loaded with the map before any combatants join.
        var fixtures = EntityQueryEnumerator<TransformComponent, DamageableComponent>();
        while (fixtures.MoveNext(out var uid, out var transform, out _))
        {
            if (transform.MapID == args.Map)
                EnsureComp<ThunderdomeProtectedComponent>(uid);
        }

        rule.Comp.NextSweep = Timing.CurTime + SweepInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var rules = EntityQueryEnumerator<ThunderdomeRuleComponent, ActiveGameRuleComponent>();
        while (rules.MoveNext(out _, out var rule, out _))
        {
            if (rule.Map is not { } map || Timing.CurTime < rule.NextSweep)
                continue;

            rule.NextSweep = Timing.CurTime + SweepInterval;
            var loose = EntityQueryEnumerator<ThunderdomeArenaItemComponent, TransformComponent>();
            while (loose.MoveNext(out var uid, out var item, out var transform))
            {
                if (transform.MapID != map)
                    continue;

                if (_containers.IsEntityOrParentInContainer(uid, xform: transform))
                    item.DeleteAfter = null;
                else if (item.DeleteAfter is { } deadline && deadline <= Timing.CurTime)
                    QueueDel(uid);
                else
                    item.DeleteAfter ??= Timing.CurTime + CleanupDelay;
            }

            var puddles = EntityQueryEnumerator<PuddleComponent, TransformComponent>();
            while (puddles.MoveNext(out var uid, out _, out var transform))
            {
                if (transform.MapID == map)
                    QueueDel(uid);
            }
        }
    }

    private void OnArenaDamage(Entity<ThunderdomeProtectedComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    protected override void Ended(Entity<ThunderdomeRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        var map = rule.Comp.Map;
        rule.Comp.Map = null;
        rule.Comp.Grids.Clear();
        CloseArena(rule.Owner);
        if (map is { } arena && _maps.MapExists(arena))
            _maps.DeleteMap(arena);
    }

    internal bool TryGetArena(out Entity<ThunderdomeRuleComponent> arena)
    {
        var rules = EntityQueryEnumerator<ThunderdomeRuleComponent>();
        while (rules.MoveNext(out var uid, out var rule))
        {
            if (rule.Map is { } map && _maps.MapExists(map) && GameTicker.IsGameRuleActive(uid))
            {
                arena = (uid, rule);
                return true;
            }
        }

        if (GameTicker.StartGameRule(RulePrototype, out var created) && created is { } ruleEntity &&
            TryComp<ThunderdomeRuleComponent>(ruleEntity.Owner, out var component) &&
            component.Map is { } createdMap && _maps.MapExists(createdMap))
        {
            arena = (ruleEntity.Owner, component);
            return true;
        }

        arena = default;
        return false;
    }

    internal EntityCoordinates? PickSpawn(Entity<ThunderdomeRuleComponent> arena)
    {
        var spawns = new List<EntityUid>();
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (points.MoveNext(out var uid, out var spawn, out var transform))
        {
            if (spawn.SpawnType == SpawnPointType.LateJoin && transform.GridUid is { } grid && arena.Comp.Grids.Contains(grid))
                spawns.Add(uid);
        }

        return spawns.Count == 0 ? null : _transform.GetMoverCoordinates(RobustRandom.Pick(spawns));
    }
}

[RegisterComponent]
public sealed partial class ThunderdomeProtectedComponent : Component;
