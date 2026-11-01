using System.Linq;
using Content.Arcane.Shared.JoinQueue;
using Content.Goobstation.Common.CCVar;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.JoinQueue;

/// <summary>
/// Defers only entry to the game. The existing connection manager still owns all admission restrictions.
/// </summary>
public sealed partial class JoinQueueSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IConnectionManager _connection = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IGameMapManager _maps = default!;
    [Dependency] private ServerGameTicker _ticker = default!;

    private static readonly TimeSpan ReconnectGrace = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan InfoInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ScoreInterval = TimeSpan.FromSeconds(1);

    private readonly JoinQueueState<ICommonSession> _queue = new();
    private readonly Dictionary<NetUserId, ICommonSession> _pending = new();
    private readonly Dictionary<NetUserId, Dictionary<QueueMiniGameKind, ScoreState>> _scores = new();
    private readonly Dictionary<NetUserId, string> _scoreNames = new();
    private readonly Dictionary<NetUserId, WaitRecord> _waits = new();
    private readonly Queue<double> _recentWaits = new();
    private TimeSpan _nextInfo;
    private TimeSpan _nextScoreBroadcast;
    private TimeSpan _nextPrune;
    private bool _scoresDirty;
    private bool _enabled;
    private bool _shutdown;

    internal int PlayerInQueueCount => _queue.Entries.Count(e => e.Session != null);
    internal int ActualPlayersCount => _players.Sessions.Count(IsAdmitted);
    internal double AverageQueueWait => _recentWaits.Count > 0 ? _recentWaits.Average() : -1;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<QueueUpdateMessage>();
        _net.RegisterNetMessage<QueueMiniGameScoreMessage>(OnScore);
        SubscribeLocalEvent<PlayerJoinRequestedEvent>(OnJoinRequested);
        SubscribeLocalEvent<GetAdmittedPlayerCountEvent>(OnCountRequested);
        _userDb.AddOnFinishLoad(OnDataLoaded);
        _players.PlayerStatusChanged += OnStatusChanged;
        _admins.OnPermsChanged += OnPermissionsChanged;
        Subs.CVar(_cfg, GoobCVars.QueueEnabled, OnEnabled, true);
        Subs.CVar(_cfg, CCVars.SoftMaxPlayers, _ => ProcessQueue());
        Subs.CVar(_cfg, CCVars.AdminsCountForMaxPlayers, _ => ProcessQueue());
    }

    public override void Shutdown()
    {
        // UserDb's callbacks belong to its server lifetime and have no removal API.
        // Pending completions must therefore explicitly stop using this system on shutdown.
        _shutdown = true;
        _players.PlayerStatusChanged -= OnStatusChanged;
        _admins.OnPermsChanged -= OnPermissionsChanged;
        _queue.Clear();
        _pending.Clear();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_enabled)
            return;
        var now = _timing.RealTime;
        if (now >= _nextPrune)
        {
            _queue.Prune(now);
            _nextPrune = now + ScoreInterval;
        }
        if (now < _nextInfo && (!_scoresDirty || now < _nextScoreBroadcast))
            return;
        SendUpdates();
    }

    private void OnJoinRequested(ref PlayerJoinRequestedEvent args)
    {
        if (!_enabled || args.Deferred)
            return;
        _queue.Connect(args.Session.UserId, args.Session, _timing.RealTime);
        args.Deferred = true;
        SendUpdates();
    }

    private void OnCountRequested(ref GetAdmittedPlayerCountEvent args)
    {
        if (_enabled)
            args.Count = ActualPlayersCount;
    }

    private void OnDataLoaded(ICommonSession session)
    {
        _tasks.RunOnMainThread(() =>
        {
            if (!_shutdown && _enabled && _queue.IsCurrent(session.UserId, session) && IsCurrentConnected(session))
                LoadPrivileges(session);
        });
    }

    private async void LoadPrivileges(ICommonSession session)
    {
        try
        {
            var privileged = await _connection.HasPrivilegedJoin(session.UserId);
            _tasks.RunOnMainThread(() => FinishDataLoaded(session, privileged));
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to determine queue admission for {session.UserId}: {exception}");
            _tasks.RunOnMainThread(() => FinishDataLoaded(session, false));
        }
    }

    private void FinishDataLoaded(ICommonSession session, bool privileged)
    {
        if (_shutdown || !_queue.IsCurrent(session.UserId, session) || !IsCurrentConnected(session))
            return;
        if (!_queue.MarkReady(session.UserId, session))
            return;
        if (privileged && _queue.Remove(session.UserId, session, out var entry))
            Admit(entry, countTowardsEstimate: false);
        ProcessQueue();
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        if (args.NewStatus == SessionStatus.Disconnected)
        {
            _queue.Disconnect(session.UserId, session, _timing.RealTime + ReconnectGrace);
            if (_pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending, session))
                _pending.Remove(session.UserId);
            ClearScores(session.UserId);
            ProcessQueue();
        }
        else if (args.NewStatus == SessionStatus.InGame)
        {
            if (_queue.Remove(session.UserId, session, out var queued))
            {
                RecordWait(queued);
                ClearScores(session.UserId);
            }
            if (_pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending, session))
                _pending.Remove(session.UserId);
            ProcessQueue();
        }
    }

    private void OnPermissionsChanged(AdminPermsChangedEventArgs args) => ProcessQueue();

    private void OnEnabled(bool enabled)
    {
        _enabled = enabled;
        if (enabled)
        {
            ProcessQueue();
            return;
        }
        var sessions = _queue.Entries.Select(e => e.Session).OfType<ICommonSession>().ToArray();
        _queue.Clear();
        foreach (var session in sessions)
        {
            ClearScores(session.UserId);
            if (IsCurrentConnected(session))
                session.Channel.Disconnect(Loc.GetString("queue-disabled-disconnect-reason"));
        }
    }

    private bool IsCurrentConnected(ICommonSession session) => session.Status == SessionStatus.Connected
        && _players.TryGetSessionById(session.UserId, out var current) && ReferenceEquals(current, session);

    private bool IsAdmitted(ICommonSession session) => session.Status == SessionStatus.InGame
        || session.Status == SessionStatus.Connected
        && _pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending, session);

    private int CountedPlayers() => _players.Sessions.Count(session => IsAdmitted(session)
        && (_cfg.GetCVar(CCVars.AdminsCountForMaxPlayers) || !_admins.IsAdmin(session)));

    private void ProcessQueue()
    {
        if (_shutdown || !_enabled)
            return;
        _queue.Prune(_timing.RealTime);
        var count = CountedPlayers();
        var maximum = Math.Max(0, _cfg.GetCVar(CCVars.SoftMaxPlayers));
        while (count < maximum && _queue.TryTake(out var entry))
        {
            if (entry.Session is not { } session || !IsCurrentConnected(session) || !Admit(entry, countTowardsEstimate: true))
                continue;
            if (_cfg.GetCVar(CCVars.AdminsCountForMaxPlayers) || !_admins.IsAdmin(session))
                count++;
        }
        SendUpdates();
    }

    private bool Admit(JoinQueueState<ICommonSession>.Entry entry, bool countTowardsEstimate)
    {
        if (entry.Session is not { } session || !IsCurrentConnected(session)
            || _pending.TryGetValue(session.UserId, out var existing) && ReferenceEquals(existing, session))
            return false;

        var waited = WaitSeconds(entry);
        RecordWait(entry);
        if (countTowardsEstimate)
        {
            _recentWaits.Enqueue(waited);
            while (_recentWaits.Count > 20)
                _recentWaits.Dequeue();
        }
        ClearScores(session.UserId);
        _pending[session.UserId] = session;
        Timer.Spawn(0, () =>
        {
            if (_shutdown || !_pending.TryGetValue(session.UserId, out var current) || !ReferenceEquals(current, session))
                return;
            if (!IsCurrentConnected(session))
            {
                _pending.Remove(session.UserId);
                return;
            }
            // Keep the reservation until the synchronous InGame status event takes over the count.
            _players.JoinGame(session);
        });
        return true;
    }

    private double WaitSeconds(JoinQueueState<ICommonSession>.Entry entry) =>
        Math.Max(0, (_timing.RealTime - entry.WaitedSince).TotalSeconds);

    private void RecordWait(JoinQueueState<ICommonSession>.Entry entry)
    {
        if (entry.Session is not { } session)
            return;
        var waited = (float) WaitSeconds(entry);
        if (_waits.TryGetValue(entry.User, out var previous))
            waited = Math.Max(waited, previous.Seconds);
        _waits[entry.User] = new WaitRecord(session.Name, waited);
    }

    private void SendUpdates()
    {
        if (_shutdown || !_enabled)
            return;
        _ticker.GetAdmittedPlayerCount();
        var entries = _queue.Entries.Where(e => e.Session is { } session && IsCurrentConnected(session)).ToArray();
        foreach (var entry in entries)
            RecordWait(entry);
        var longest = _waits.OrderByDescending(p => p.Value.Seconds).Take(100).ToArray();
        if (_waits.Count > 200 + entries.Length)
        {
            var retain = longest.Select(p => p.Key).Concat(entries.Select(e => e.User)).ToHashSet();
            foreach (var user in _waits.Keys.ToArray())
                if (!retain.Contains(user))
                    _waits.Remove(user);
        }

        var names = entries.Select(e => e.Session!.Name).Take(1000).ToList();
        var waits = entries.Take(1000).Select(e => (float) WaitSeconds(e)).ToList();
        var board = BuildScores();
        var average = AverageQueueWait;
        var preset = _ticker.CurrentPreset ?? _ticker.Preset;
        var mode = preset == null ? string.Empty : preset.ModeTitle.Id;
        var elapsed = _ticker.RunLevel >= GameRunLevel.InRound
            ? (int) (_timing.CurTime - _ticker.RoundStartTimeSpan).TotalMinutes : 0;
        var mapName = _maps.GetSelectedMap()?.MapName ?? string.Empty;
        var playerCount = ActualPlayersCount;
        var maximumPlayers = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var leaderboardNames = longest.Select(p => p.Value.Name).ToList();
        var leaderboardSeconds = longest.Select(p => p.Value.Seconds).ToList();
        for (var i = 0; i < entries.Length; i++)
        {
            var session = entries[i].Session!;
            session.Channel.SendMessage(new QueueUpdateMessage
            {
                Total = entries.Length,
                Position = i + 1,
                EstimatedWaitSeconds = average < 0 ? -1 : (float) (average * (i + 1) / entries.Length),
                MapName = mapName,
                GameMode = mode,
                ServerPlayerCount = playerCount,
                MaxPlayerCount = maximumPlayers,
                RoundDurationMinutes = elapsed,
                YourName = session.Name,
                PlayerNames = names,
                PlayerWaitSeconds = waits,
                QueueWaitLeaderboardNames = leaderboardNames,
                QueueWaitLeaderboardSeconds = leaderboardSeconds,
                MiniGameLeaderboard = board,
            });
        }
        _scoresDirty = false;
        _nextInfo = _timing.RealTime + InfoInterval;
        _nextScoreBroadcast = _timing.RealTime + ScoreInterval;
    }

    private void OnScore(QueueMiniGameScoreMessage message)
    {
        var maximum = GetMaxScore(message.Game);
        if (!_enabled || !_queue.TryGet(message.MsgChannel.UserId, out var entry)
            || entry.Session is not { } session || !ReferenceEquals(session.Channel, message.MsgChannel)
            || !IsCurrentConnected(session) || maximum == 0)
            return;
        if (!_scores.TryGetValue(entry.User, out var scores))
            _scores[entry.User] = scores = new();
        var now = _timing.RealTime;
        if (scores.TryGetValue(message.Game, out var previous) && now - previous.UpdatedAt < ScoreInterval)
            return;
        var score = Math.Max(previous.Score, Math.Clamp(message.Score, 0, maximum));
        scores[message.Game] = new ScoreState(score, now);
        _scoreNames[entry.User] = session.Name;
        if (score != previous.Score)
            _scoresDirty = true;
    }

    internal List<QueueMiniGameLeaderboardEntry> BuildScores()
    {
        var board = new List<QueueMiniGameLeaderboardEntry>(15);
        foreach (var game in Enum.GetValues<QueueMiniGameKind>())
        {
            var best = _scores.Where(p => p.Value.TryGetValue(game, out var state) && state.Score > 0)
                .OrderByDescending(p => p.Value[game].Score).Take(5);
            board.AddRange(best.Select(p => new QueueMiniGameLeaderboardEntry(game, _scoreNames[p.Key], p.Value[game].Score)));
        }
        return board;
    }

    private void ClearScores(NetUserId user)
    {
        _scoresDirty |= _scores.Remove(user);
        _scoreNames.Remove(user);
    }

    internal static int GetMaxScore(QueueMiniGameKind game) => game switch
    {
        QueueMiniGameKind.Gyruss => 5000,
        QueueMiniGameKind.GoGoShitcurity => 10000,
        QueueMiniGameKind.SpaceInvaders => 6000,
        _ => 0,
    };

    private readonly record struct ScoreState(int Score, TimeSpan UpdatedAt);
    private readonly record struct WaitRecord(string Name, float Seconds);
}
