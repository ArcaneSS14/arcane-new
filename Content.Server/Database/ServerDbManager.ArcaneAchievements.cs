// Arcane-Start
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<AchievementProgress>> GetAchievementProgressAsync(NetUserId userId, CancellationToken cancel = default);

    /// <summary>Retry a failed write with the same immutable batch until success is known.</summary>
    Task AddAchievementProgressAsync(NetUserId userId, string achievementId, Guid batchId, int delta, DateTime? unlockedAt);

    /// <summary>Release a known-successful batch. Its ID must never be retried after acknowledgement.</summary>
    Task AcknowledgeAchievementBatchAsync(NetUserId userId, Guid batchId);

    Task<(int Players, Dictionary<string, int> Unlocks)> GetAchievementStatsAsync(CancellationToken cancel = default);
}

public sealed partial class ServerDbManager
{
    public Task<List<AchievementProgress>> GetAchievementProgressAsync(NetUserId userId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAchievementProgressAsync(userId, cancel));
    }

    public Task AddAchievementProgressAsync(NetUserId userId, string achievementId, Guid batchId, int delta, DateTime? unlockedAt)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddAchievementProgressAsync(userId, achievementId, batchId, delta, unlockedAt));
    }

    public Task AcknowledgeAchievementBatchAsync(NetUserId userId, Guid batchId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AcknowledgeAchievementBatchAsync(userId, batchId));
    }

    public Task<(int Players, Dictionary<string, int> Unlocks)> GetAchievementStatsAsync(CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAchievementStatsAsync(cancel));
    }
}
// Arcane-End
