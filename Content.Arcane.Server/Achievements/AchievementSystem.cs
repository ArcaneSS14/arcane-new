using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Achievements;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Events;
using Content.Server.Maps;
using Content.Shared.EntityConditions;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Achievements;

public sealed partial class AchievementSystem : EntitySystem
{
    [Dependency] private AchievementManager _manager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IGameMapManager _gameMap = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    private bool _enabled;
    private bool _roundExcluded;
    private bool _roundEnded;
    private TimeSpan _saveInterval;
    private TimeSpan _nextSave;
    private readonly Dictionary<ICommonSession, TimeSpan> _requests = new();

    public event Action<ICommonSession>? SessionLoaded;
    public event Action<ICommonSession, AchievementPrototype>? AchievementUnlocked;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestAchievementsEvent>(OnRequest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEndMessage);

        Subs.CVar(_cfg, ArcaneCVars.AchievementsEnabled, value => _enabled = value, true);
        Subs.CVar(_cfg, ArcaneCVars.AchievementsSaveInterval, value =>
            _saveInterval = TimeSpan.FromSeconds(float.IsFinite(value) && value is >= 1 and <= 86400 ? value : 60), true);

        _manager.Unlocked += OnUnlocked;
        _manager.PlayerLoaded += OnPlayerLoaded;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        _ = _manager.RefreshStats();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _manager.Unlocked -= OnUnlocked;
        _manager.PlayerLoaded -= OnPlayerLoaded;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
        _requests.Clear();
        _ = _manager.SaveAll();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextSave)
            return;

        _nextSave = _timing.CurTime + _saveInterval;
        _ = _manager.SaveAll();
    }

    private void OnRequest(RequestAchievementsEvent ev, EntitySessionEventArgs args)
    {
        if (!_manager.IsLoaded(args.SenderSession)
            || _requests.TryGetValue(args.SenderSession, out var next) && _timing.RealTime < next)
            return;
        _requests[args.SenderSession] = _timing.RealTime + TimeSpan.FromSeconds(1);
        RaiseNetworkEvent(new AchievementsStateEvent(_manager.BuildState(args.SenderSession)), args.SenderSession);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _requests.Remove(args.Session);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var presets = _cfg.GetCVar(ArcaneCVars.AchievementsExcludedPresets).Split(',', StringSplitOptions.TrimEntries);
        var maps = _cfg.GetCVar(ArcaneCVars.AchievementsExcludedMaps).Split(',', StringSplitOptions.TrimEntries);

        _roundExcluded = (_ticker.CurrentPreset ?? _ticker.Preset) is { } preset && Array.IndexOf(presets, preset.ID) >= 0
                         || _gameMap.GetSelectedMap() is { } map && Array.IndexOf(maps, map.ID) >= 0;

        _roundEnded = false;
        _ = _manager.RefreshStats();
    }

    private void OnRoundEndMessage(RoundEndMessageEvent ev)
    {
        _roundEnded = true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _ = _manager.SaveAll();
    }

    private void OnUnlocked(ICommonSession session, AchievementPrototype proto)
    {
        RaiseNetworkEvent(new AchievementUnlockedEvent(_manager.BuildData(session, proto)), session);
        AchievementUnlocked?.Invoke(session, proto);
    }

    private void OnPlayerLoaded(ICommonSession session)
    {
        SessionLoaded?.Invoke(session);
        RaiseNetworkEvent(new AchievementsStateEvent(_manager.BuildState(session)), session);
    }

    public EntityUid ResolveAttacker(EntityUid origin)
    {
        return TryComp<ProjectileComponent>(origin, out var projectile) && projectile.Shooter is { } shooter
            ? shooter
            : origin;
    }

    public bool TryGetSession(EntityUid player, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return _mind.TryGetMind(player, out _, out var mind)
               && _player.TryGetSessionById(mind.UserId, out session);
    }

    public bool CanProgress(EntityUid player, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        if (!_enabled
            || _roundExcluded
            || _roundEnded
            || HasComp<GhostComponent>(player)
            || HasComp<NoAchievementsComponent>(player)
            || !_mind.TryGetMind(player, out var mindId, out var mind)
            || !_player.TryGetSessionById(mind.UserId, out session))
            return false;

        return _roles.MindHasRole<JobRoleComponent>(mindId)
               || _roles.MindHasRole<GhostRoleMarkerRoleComponent>(mindId)
               || _roles.MindIsAntagonist(mindId);
    }

    public bool CanProgress(ICommonSession session)
    {
        return _enabled && !HasComp<NoAchievementsComponent>(session.AttachedEntity) && _manager.IsLoaded(session);
    }

    public void AddProgress(EntityUid player, AchievementPrototype proto, int amount = 1)
    {
        if (!CanProgress(player, out var session)
            || proto.Condition.Requirements is { } requirements && !_conditions.TryConditions(player, requirements))
            return;

        _manager.AddProgress(session, proto, amount);
    }

    public void AddProgress(ICommonSession session, AchievementPrototype proto, int amount = 1)
    {
        if (!CanProgress(session) || _roundExcluded || _roundEnded)
            return;

        _manager.AddProgress(session, proto, amount);
    }

    public void SetDerivedProgress(ICommonSession session, AchievementPrototype proto, int value)
    {
        if (!CanProgress(session))
            return;

        _manager.SetDerivedProgress(session, proto, value);
    }

    public bool IsUnlocked(ICommonSession session, AchievementPrototype proto)
    {
        return _manager.IsUnlocked(session, proto.ID);
    }

    public int CountUnlocked(ICommonSession session, ProtoId<AchievementCategoryPrototype>? category = null)
    {
        return _manager.CountUnlocked(session, category);
    }
}
