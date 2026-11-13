using Content.Arcane.Shared.Achievements;
using Content.Goobstation.Common.CCVar;
using Content.Shared.EntityConditions;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class CausedMobStateCondition : AchievementCondition
{
    [DataField]
    public List<MobState> MobStates = new() { MobState.Dead };

    [DataField]
    public EntityCondition[]? TargetRequirements;
}

public sealed partial class KilledBySameCondition : AchievementCondition
{
    [DataField]
    public int Kills = 3;
}

public sealed partial class KillRageQuitCondition : AchievementCondition;

public sealed partial class KillConditionsSystem : EntitySystem
{
    [Dependency] private AchievementSystem _achievements = default!;
    [Dependency] private CausedMobStateConditionSystem _caused = default!;
    [Dependency] private KilledBySameConditionSystem _killedBySame = default!;
    [Dependency] private KillRageQuitConditionSystem _rageQuit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Origin is not { } origin || args.NewMobState is not (MobState.Critical or MobState.Dead)
            || args.OldMobState is MobState.Invalid or MobState.Dead
            || args.OldMobState == args.NewMobState || HasComp<NoAchievementsComponent>(args.Target))
            return;

        var attacker = _achievements.ResolveAttacker(origin);
        if (attacker == args.Target || HasComp<NoAchievementsComponent>(attacker))
            return;

        _caused.OnCaused(attacker, args.Target, args.NewMobState);
        _rageQuit.OnCaused(attacker, args.Target);

        if (args.NewMobState == MobState.Dead)
            _killedBySame.OnKilled(attacker, args.Target);
    }
}

public sealed partial class CausedMobStateConditionSystem : AchievementConditionSystem<CausedMobStateCondition>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public void OnCaused(EntityUid attacker, EntityUid target, MobState state)
    {
        foreach (var (proto, condition) in Entries)
        {
            if (!condition.MobStates.Contains(state)
                || condition.TargetRequirements is { } requirements && !_conditions.TryConditions(target, requirements))
                continue;

            Achievements.AddProgress(attacker, proto);
        }
    }
}

public sealed partial class KilledBySameConditionSystem : AchievementConditionSystem<KilledBySameCondition>
{
    private readonly Dictionary<(NetUserId Victim, NetUserId Killer), int> _kills = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public void OnKilled(EntityUid killer, EntityUid victim)
    {
        if (Entries.Count == 0
            || !Achievements.TryGetSession(killer, out var killerSession)
            || !Achievements.TryGetSession(victim, out var victimSession)
            || killerSession == victimSession)
            return;

        var key = (victimSession.UserId, killerSession.UserId);
        var count = _kills.GetValueOrDefault(key) + 1;
        _kills[key] = count;

        foreach (var (proto, condition) in Entries)
        {
            if (count == condition.Kills)
                Achievements.AddProgress(victim, proto);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _kills.Clear();
    }
}

public sealed partial class KillRageQuitConditionSystem : AchievementConditionSystem<KillRageQuitCondition>
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, LastAttack> _lastAttacker = new();
    private TimeSpan _window;

    public override void Initialize()
    {
        base.Initialize();

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        Subs.CVar(_cfg, GoobCVars.PlayerRageQuitTimeThreshold, value =>
            _window = TimeSpan.FromSeconds(float.IsFinite(value) && value is >= 0 and <= 86400 ? value : 30), true);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public void OnCaused(EntityUid attacker, EntityUid victim)
    {
        if (Entries.Count > 0 && Achievements.TryGetSession(victim, out var victimSession)
            && Achievements.CanProgress(attacker, out var attackerSession))
            _lastAttacker[victimSession.UserId] = new LastAttack(attackerSession.UserId, _timing.RealTime + _window);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected
            || !_lastAttacker.Remove(args.Session.UserId, out var attack)
            || _timing.RealTime > attack.Expires
            || !_players.TryGetSessionById(attack.Killer, out var killer))
            return;

        foreach (var (proto, _) in Entries)
            Achievements.AddProgress(killer, proto);
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        base.Shutdown();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastAttacker.Clear();
    }

    private readonly record struct LastAttack(NetUserId Killer, TimeSpan Expires);
}
