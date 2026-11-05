// Arcane-Start
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<bool> MarkPollSeenAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.MarkPollSeenAsync(pollId, userId, cancel));
    }

    public Task<HashSet<int>> GetSeenPollIdsAsync(NetUserId userId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSeenPollIdsAsync(userId, cancel));
    }

    public Task<int> GetPollSeenCountAsync(int pollId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPollSeenCountAsync(pollId, cancel));
    }
}
// Arcane-End
