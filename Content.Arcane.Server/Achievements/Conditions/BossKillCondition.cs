using System.Linq;
using Content.Lavaland.Shared.Aggression;
using Content.Arcane.Shared.Achievements;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class BossKillCondition : AchievementCondition
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public List<EntProtoId>? Prototypes;
}

public sealed partial class BossKillConditionSystem : AchievementConditionSystem<BossKillCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AggressiveComponent, AggressorAddedEvent>(OnAggressorAdded);
        SubscribeLocalEvent<AggressiveComponent, AggressorRemovedEvent>(OnAggressorRemoved);
        SubscribeLocalEvent<DamageableComponent, MobStateChangedEvent>(OnStateChanged, before: [typeof(AggressorsSystem)]);
    }

    private bool Matches(EntityUid boss, BossKillCondition condition) =>
        _whitelist.IsWhitelistPassOrNull(condition.Whitelist, boss)
        && (condition.Prototypes == null || Prototype(boss) is { } proto && condition.Prototypes.Contains(proto.ID));

    private void OnAggressorAdded(Entity<AggressiveComponent> ent, ref AggressorAddedEvent args)
    {
        if (TryComp<MobStateComponent>(ent, out var state) && state.CurrentState == MobState.Dead)
            return;
        Capture(ent.Owner, args.Aggressor);
    }

    private void Capture(EntityUid boss, EntityUid attacker)
    {
        if (HasComp<NoAchievementsComponent>(boss) || !Entries.Any(entry => Matches(boss, entry.Condition))
            || !Achievements.CanProgress(attacker, out var session))
            return;
        var credit = EnsureComp<BossAchievementCreditComponent>(boss);
        if (!credit.Users.TryGetValue(attacker, out var users))
            credit.Users[attacker] = users = new HashSet<NetUserId>();
        users.Add(session.UserId);
    }

    private void OnAggressorRemoved(Entity<AggressiveComponent> ent, ref AggressorRemovedEvent args)
    {
        if (TryComp<BossAchievementCreditComponent>(ent, out var credit))
            credit.Users.Remove(args.Aggressor);
    }

    private void OnStateChanged(Entity<DamageableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead
            || HasComp<NoAchievementsComponent>(ent)
            || !TryComp<AggressiveComponent>(ent, out var aggressive))
            return;

        // Threshold handling may precede aggression bookkeeping for the final hit.
        var aggressors = new HashSet<EntityUid>(aggressive.Aggressors);
        if (args.Origin is { } origin)
        {
            var attacker = Achievements.ResolveAttacker(origin);
            Capture(ent.Owner, attacker);
            aggressors.Add(attacker);
        }
        if (!TryComp<BossAchievementCreditComponent>(ent, out var credit))
            return;

        var users = new HashSet<NetUserId>();
        foreach (var aggressor in aggressors)
        {
            if (credit.Users.TryGetValue(aggressor, out var credited))
                users.UnionWith(credited);
        }
        foreach (var (proto, condition) in Entries)
        {
            if (!Matches(ent.Owner, condition))
                continue;
            foreach (var user in users)
            {
                if (_players.TryGetSessionById(user, out var session))
                    Achievements.AddProgress(session, proto);
            }
        }
        credit.Users.Clear();
    }
}

/// <summary>Keep account identity from the attack rather than resolving a possibly replaced mind at death.</summary>
[RegisterComponent]
public sealed partial class BossAchievementCreditComponent : Component
{
    public readonly Dictionary<EntityUid, HashSet<NetUserId>> Users = new();
}
