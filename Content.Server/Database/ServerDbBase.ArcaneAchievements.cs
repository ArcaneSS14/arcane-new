// Arcane-Start
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<List<AchievementProgress>> GetAchievementProgressAsync(NetUserId userId, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);

        var list = await db.DbContext.AchievementProgress
            .AsNoTracking()
            .Where(a => a.PlayerUserId == userId.UserId)
            .ToListAsync(cancel);

        foreach (var entry in list)
        {
            entry.UnlockedAt = NormalizeDatabaseTime(entry.UnlockedAt);
        }

        return list;
    }

    public async Task AddAchievementProgressAsync(NetUserId userId, string achievementId, Guid batchId, int delta, DateTime? unlockedAt)
    {
        if (delta < 0)
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (string.IsNullOrWhiteSpace(achievementId) || achievementId.Length > 128)
            throw new ArgumentException("An achievement ID must contain 1 to 128 characters.", nameof(achievementId));
        if (batchId == Guid.Empty)
            throw new ArgumentException("A write batch must have a unique ID.", nameof(batchId));

        await using var db = await GetDb();
        var context = db.DbContext;
        if (await IsAppliedAchievementBatch(context, userId, achievementId, batchId, delta, unlockedAt != null))
            return;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                context.AchievementProgressBatches.Add(new AchievementProgressBatch
                {
                    Id = batchId,
                    PlayerUserId = userId.UserId,
                    AchievementId = achievementId,
                    Delta = delta,
                    MarksUnlocked = unlockedAt != null,
                });
                await context.SaveChangesAsync();

                if (!await TryIncrementAchievement(context, userId.UserId, achievementId, delta, unlockedAt))
                {
                    context.AchievementProgress.Add(new AchievementProgress
                    {
                        PlayerUserId = userId.UserId,
                        AchievementId = achievementId,
                        Progress = delta,
                        UnlockedAt = unlockedAt,
                    });
                    await context.SaveChangesAsync();
                }

                // The claim and increment commit together, including an ambiguous commit acknowledgement.
                await transaction.CommitAsync();
                return;
            }
            catch (DbUpdateException)
            {
                // The transaction is disposed before checking a concurrent winner on PostgreSQL.
                context.ChangeTracker.Clear();
                if (await IsAppliedAchievementBatch(context, userId, achievementId, batchId, delta, unlockedAt != null))
                    return;
                // Another batch may have created the first progress row. Retry that race once.
                if (attempt != 0 || !await context.AchievementProgress.AnyAsync(a =>
                        a.PlayerUserId == userId.UserId && a.AchievementId == achievementId))
                    throw;
            }
        }
    }

    private static async Task<bool> IsAppliedAchievementBatch(ServerDbContext context, NetUserId user,
        string achievement, Guid batchId, int delta, bool marksUnlocked)
    {
        var batch = await context.AchievementProgressBatches.AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == batchId);
        if (batch == null)
            return false;
        if (batch.PlayerUserId != user.UserId || batch.AchievementId != achievement
            || batch.Delta != delta || batch.MarksUnlocked != marksUnlocked)
            throw new InvalidOperationException("An achievement batch ID was reused for another write.");
        return true;
    }

    public async Task AcknowledgeAchievementBatchAsync(NetUserId userId, Guid batchId)
    {
        await using var db = await GetDb();
        await db.DbContext.AchievementProgressBatches
            .Where(b => b.Id == batchId && b.PlayerUserId == userId.UserId).ExecuteDeleteAsync();
    }

    private static async Task<bool> TryIncrementAchievement(
        ServerDbContext context,
        Guid userId,
        string achievementId,
        int delta,
        DateTime? unlockedAt)
    {
        var updated = await context.AchievementProgress
            .Where(a => a.PlayerUserId == userId && a.AchievementId == achievementId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(a => a.Progress, a => a.Progress + delta)
                .SetProperty(a => a.UnlockedAt, a => a.UnlockedAt ?? unlockedAt));

        return updated > 0;
    }

    public async Task<(int Players, Dictionary<string, int> Unlocks)> GetAchievementStatsAsync(CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);

        var players = await db.DbContext.AchievementProgress
            .Select(a => a.PlayerUserId)
            .Distinct()
            .CountAsync(cancel);

        var unlocks = await db.DbContext.AchievementProgress
            .Where(a => a.UnlockedAt != null)
            .GroupBy(a => a.AchievementId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Id, g => g.Count, cancel);

        return (players, unlocks);
    }
}
// Arcane-End
