using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<List<ulong>> GetDiscordRoleIds(Guid player, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        return await (
                from linked in db.DbContext.RMCLinkedAccounts
                join role in db.DbContext.Set<ArcaneDiscordAccountRole>() on linked.DiscordId equals role.DiscordId
                where linked.PlayerId == player
                select role.RoleId)
            .ToListAsync(cancel);
    }

    public async Task<bool> HasDiscordRole(Guid player, ulong roleId, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        return await (
                from linked in db.DbContext.RMCLinkedAccounts
                join role in db.DbContext.Set<ArcaneDiscordAccountRole>() on linked.DiscordId equals role.DiscordId
                where linked.PlayerId == player && role.RoleId == roleId
                select role)
            .AnyAsync(cancel);
    }
}
