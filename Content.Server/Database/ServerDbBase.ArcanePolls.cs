// Arcane-Start
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<bool> MarkPollSeenAsync(int pollId, NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var context = db.DbContext;
        var now = DateTime.UtcNow;
        if (!await context.Polls.AnyAsync(p => p.Id == pollId && p.Active && p.StartTime <= now
                && (p.EndTime == null || p.EndTime > now), cancel)
            || !await context.Player.AnyAsync(p => p.UserId == userId.UserId, cancel)
            || await context.PollSeen.AnyAsync(s => s.PollId == pollId && s.PlayerUserId == userId.UserId, cancel))
            return false;

        context.PollSeen.Add(new PollSeen { PollId = pollId, PlayerUserId = userId.UserId, SeenAt = now });
        try
        {
            await context.SaveChangesAsync(cancel);
            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent request may have inserted the unique pair. Do not hide other DB failures.
            if (await context.PollSeen.AnyAsync(s => s.PollId == pollId && s.PlayerUserId == userId.UserId, cancel))
                return false;
            throw;
        }
    }

    public async Task<HashSet<int>> GetSeenPollIdsAsync(NetUserId userId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        return (await db.DbContext.PollSeen.Where(s => s.PlayerUserId == userId.UserId)
            .Select(s => s.PollId).ToListAsync(cancel)).ToHashSet();
    }

    public async Task<int> GetPollSeenCountAsync(int pollId, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        return await db.DbContext.PollSeen.CountAsync(s => s.PollId == pollId, cancel);
    }
}
// Arcane-End
