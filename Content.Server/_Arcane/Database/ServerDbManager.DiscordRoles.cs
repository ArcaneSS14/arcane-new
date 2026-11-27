// Arcane-Start
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    /// <summary>
    /// Discord role ids of the Discord account linked to this player, empty when none is linked.
    /// </summary>
    Task<List<ulong>> GetDiscordRoleIds(Guid player, CancellationToken cancel);

    Task<bool> HasDiscordRole(Guid player, ulong roleId, CancellationToken cancel);
}

public sealed partial class ServerDbManager
{
    public Task<List<ulong>> GetDiscordRoleIds(Guid player, CancellationToken cancel)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetDiscordRoleIds(player, cancel));
    }

    public Task<bool> HasDiscordRole(Guid player, ulong roleId, CancellationToken cancel)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.HasDiscordRole(player, roleId, cancel));
    }
}
// Arcane-End
