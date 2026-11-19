using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Arcane.Shared.Achievements;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements;

/// <summary>Owns loaded progress on the server synchronization context used by UserDb callbacks.</summary>
public sealed partial class AchievementManager : EntitySystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ITaskManager _tasks = default!;

    private readonly Dictionary<NetUserId, PlayerProgress> _players = new();
    private readonly HashSet<Task> _cleanupTasks = new();
    private Dictionary<string, int> _unlockCounts = new();
    private int _playerCount;
    private bool _shutdown;
    private bool _finalSaveComplete;

    public event Action<ICommonSession, AchievementPrototype>? Unlocked;
    public event Action<ICommonSession>? PlayerLoaded;

    public override void Initialize()
    {
        base.Initialize();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }

    public override void Shutdown()
    {
        FlushForShutdown();
        base.Shutdown();
    }

    public void FlushForShutdown()
    {
        if (_finalSaveComplete)
            return;
        // UserDb callbacks have server lifetime and no removal API.
        _shutdown = true;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var save = SaveAll(drain: true);
            _tasks.BlockWaitOnTask(save);
            if (!save.GetAwaiter().GetResult())
                continue;
            _finalSaveComplete = true;
            return;
        }
        Log.Error("Achievement progress could not be fully saved after three shutdown attempts; database access is required.");
    }

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        if (_shutdown)
            throw new OperationCanceledException(cancel);
        if (!_players.TryGetValue(session.UserId, out var profile))
            _players[session.UserId] = profile = new PlayerProgress();
        profile.Session = session;
        profile.Loaded = false;

        // A quick reconnect must not read before the previous session's writes finish.
        if (!await FlushProfile(session.UserId, profile, drain: true))
            throw new InvalidOperationException("Previous achievement progress could not be saved.");

        var rows = await _db.GetAchievementProgressAsync(session.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        if (_shutdown || !ReferenceEquals(profile.Session, session))
            throw new OperationCanceledException(cancel);
        profile.Entries.Clear();
        foreach (var row in rows)
        {
            profile.Entries[row.AchievementId] = new AchievementEntry
            {
                Progress = Math.Max(0, row.Progress),
                UnlockedAt = row.UnlockedAt,
            };
        }
        profile.Loaded = true;
    }

    private void FinishLoad(ICommonSession session)
    {
        if (IsLoaded(session))
            PlayerLoaded?.Invoke(session);
    }

    private void ClientDisconnected(ICommonSession session)
    {
        if (_shutdown || !_players.TryGetValue(session.UserId, out var profile)
            || !ReferenceEquals(profile.Session, session))
            return;
        profile.Session = null;
        profile.Loaded = false;
        _ = FlushProfile(session.UserId, profile, drain: true);
    }

    public bool IsLoaded(ICommonSession session) => !_shutdown
        && _players.TryGetValue(session.UserId, out var profile)
        && ReferenceEquals(profile.Session, session) && profile.Loaded;

    public bool IsUnlocked(ICommonSession session, string id) => IsLoaded(session)
        && _players[session.UserId].Entries.TryGetValue(id, out var entry) && entry.UnlockedAt != null;

    public int CountUnlocked(ICommonSession session, ProtoId<AchievementCategoryPrototype>? category = null)
    {
        if (!IsLoaded(session))
            return 0;
        var count = 0;
        foreach (var (id, entry) in _players[session.UserId].Entries)
        {
            if (entry.UnlockedAt == null || !_proto.TryIndex<AchievementPrototype>(id, out var proto))
                continue;
            if (category == null || proto.Category == category)
                count++;
        }
        return count;
    }

    public void AddProgress(ICommonSession session, AchievementPrototype proto, int amount)
    {
        if (amount <= 0 || !TryGetEntry(session, proto, out var entry) || entry.UnlockedAt != null)
            return;
        var added = (int) Math.Min(amount, Math.Max(0L, (long) proto.Condition.Count - entry.Progress));
        entry.Progress += added;
        entry.PendingDelta += added;
        if (entry.Progress >= proto.Condition.Count)
            Unlock(session, proto, entry);
    }

    public void SetDerivedProgress(ICommonSession session, AchievementPrototype proto, int value)
    {
        if (!TryGetEntry(session, proto, out var entry) || entry.UnlockedAt != null)
            return;
        // Derived counts are rebuilt from persisted achievements, rather than accumulated twice.
        entry.Progress = Math.Clamp(value, 0, Math.Max(1, proto.Condition.Count));
        if (entry.Progress >= proto.Condition.Count)
            Unlock(session, proto, entry);
    }

    private bool TryGetEntry(ICommonSession session, AchievementPrototype proto, out AchievementEntry entry)
    {
        entry = default!;
        if (!IsLoaded(session))
            return false;
        var entries = _players[session.UserId].Entries;
        if (!entries.TryGetValue(proto.ID, out var found))
            entries[proto.ID] = found = new AchievementEntry();
        entry = found;
        return true;
    }

    private void Unlock(ICommonSession session, AchievementPrototype proto, AchievementEntry entry)
    {
        entry.UnlockedAt = DateTime.UtcNow;
        entry.PendingUnlock = true;
        _ = SaveEntry(session.UserId, proto.ID, entry);
        _unlockCounts[proto.ID] = _unlockCounts.GetValueOrDefault(proto.ID) + 1;
        Log.Info($"{session.Name} unlocked achievement {proto.ID}");
        Unlocked?.Invoke(session, proto);
    }

    public async Task<bool> SaveAll(bool drain = false)
    {
        // A synchronous flush can prune an offline profile, so snapshot before starting it.
        var tasks = _players.ToArray().Select(pair => FlushProfile(pair.Key, pair.Value, drain)).ToArray();
        var saved = (await Task.WhenAll(tasks)).All(success => success);
        await Task.WhenAll(_cleanupTasks.ToArray());
        _cleanupTasks.RemoveWhere(task => task.IsCompleted);
        return saved;
    }

    private async Task<bool> FlushProfile(NetUserId user, PlayerProgress profile, bool drain)
    {
        do
        {
            var tasks = profile.Entries.Select(pair => SaveEntry(user, pair.Key, pair.Value)).ToArray();
            if (!(await Task.WhenAll(tasks)).All(success => success))
                return false;
        } while (drain && profile.Entries.Values.Any(HasPendingWrite));

        if (profile.Session == null && !profile.Entries.Values.Any(HasPendingWrite)
            && _players.TryGetValue(user, out var current) && ReferenceEquals(current, profile))
            _players.Remove(user);
        return true;
    }

    private static bool HasPendingWrite(AchievementEntry entry) =>
        entry.Saving || entry.Batch != null || entry.PendingDelta > 0 || entry.PendingUnlock;

    private Task<bool> SaveEntry(NetUserId user, string id, AchievementEntry entry)
    {
        if (entry.Saving)
            return entry.SaveTask!;
        if (entry.Batch == null)
        {
            var unlockedAt = entry.PendingUnlock ? entry.UnlockedAt : null;
            if (entry.PendingDelta == 0 && unlockedAt == null)
                return Task.FromResult(true);
            entry.Batch = new WriteBatch(Guid.NewGuid(), entry.PendingDelta, unlockedAt);
            entry.PendingDelta = 0;
            entry.PendingUnlock = false;
        }
        entry.Saving = true;
        return entry.SaveTask = SaveAsync(user, id, entry, entry.Batch);
    }

    private async Task<bool> SaveAsync(NetUserId user, string id, AchievementEntry entry, WriteBatch batch)
    {
        Exception? failure = null;
        try
        {
            await _db.AddAchievementProgressAsync(user, id, batch.Id, batch.Delta, batch.UnlockedAt);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        entry.Saving = false;
        if (failure != null)
        {
            // An unknown commit result must retry the same immutable batch, not create another increment.
            Log.Error($"Failed to save achievement {id} for {user}; batch retained for retry: {failure}");
            return false;
        }
        entry.Batch = null;
        _cleanupTasks.Add(AcknowledgeBatch(user, batch.Id));
        // An unlock arriving during an older write should not wait for the periodic save.
        if (entry.PendingUnlock)
            _ = SaveEntry(user, id, entry);
        return true;
    }

    private async Task AcknowledgeBatch(NetUserId user, Guid batchId)
    {
        try
        {
            await _db.AcknowledgeAchievementBatchAsync(user, batchId);
        }
        catch (Exception exception)
        {
            // The progress commit is already known; a retained receipt is harmless and must not be retried as progress.
            Log.Warning($"Could not remove acknowledged achievement batch {batchId}: {exception}");
        }
    }

    public async Task RefreshStats()
    {
        try
        {
            var (players, unlocks) = await _db.GetAchievementStatsAsync();
            if (_shutdown)
                return;
            _playerCount = players;
            _unlockCounts = unlocks;
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to load achievement stats: {exception}");
        }
    }

    public AchievementData BuildData(ICommonSession session, AchievementPrototype proto)
    {
        AchievementEntry? entry = null;
        if (IsLoaded(session))
            _players[session.UserId].Entries.TryGetValue(proto.ID, out entry);
        var unlocked = entry?.UnlockedAt != null;
        var revealed = unlocked || !proto.Hidden;
        var target = Math.Max(1, proto.Condition.Count);
        return new AchievementData(proto.ID, revealed ? proto.Name.Id : null,
            revealed ? proto.Description.Id : null, revealed ? proto.Icon : null, proto.Category,
            unlocked ? target : Math.Clamp(entry?.Progress ?? 0, 0, target), target, entry?.UnlockedAt,
            _playerCount <= 0 ? 0f : Math.Clamp(_unlockCounts.GetValueOrDefault(proto.ID) / (float) _playerCount, 0f, 1f));
    }

    public List<AchievementData> BuildState(ICommonSession session) => _proto
        .EnumeratePrototypes<AchievementPrototype>().Select(proto => BuildData(session, proto)).ToList();

    private sealed class PlayerProgress
    {
        public ICommonSession? Session;
        public bool Loaded;
        public readonly Dictionary<string, AchievementEntry> Entries = new();
    }

    private sealed class AchievementEntry
    {
        public int Progress;
        public DateTime? UnlockedAt;
        public int PendingDelta;
        public bool PendingUnlock;
        public WriteBatch? Batch;
        public bool Saving;
        public Task<bool>? SaveTask;
    }

    private sealed record WriteBatch(Guid Id, int Delta, DateTime? UnlockedAt);
}
