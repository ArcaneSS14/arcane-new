using System.Threading;
using System.Threading.Tasks;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Arcane.Common.DiscordRoles;
using Content.Arcane.Shared.DiscordRoles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Arcane.Server.DiscordRoles;

public sealed partial class DiscordRoleManager : IPostInjectInit, ISharedDiscordRoleManager
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ITaskManager _task = default!;
    [Dependency] private UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, HashSet<DiscordRole>> _roles = new();
    private readonly Dictionary<NetUserId, HashSet<DiscordRole>> _roleOverrides = new();
    private readonly Dictionary<NetUserId, HashSet<DiscordRole>> _connecting = new();
    private readonly DiscordRoleIdMap _roleIds = new(DiscordRoleIds.All);

    /// <summary>
    /// Preloaded entries of players who never finished connecting are dropped past this many.
    /// </summary>
    private const int MaxConnecting = 256;

    public event Action? RolesUpdated;

    public bool HasRole(ICommonSession session, DiscordRole role)
    {
        return HasRole(session.UserId, role);
    }

    /// <summary>
    /// Whether the player has the role, from the roles loaded with their session or preloaded while connecting.
    /// </summary>
    public bool HasRole(NetUserId player, DiscordRole role)
    {
        return (_roles.TryGetValue(player, out var roles) && roles.Contains(role)) ||
               (_connecting.TryGetValue(player, out var connecting) && connecting.Contains(role)) ||
               (_roleOverrides.TryGetValue(player, out var overrides) && overrides.Contains(role));
    }

    public bool HasAnyRole(ICommonSession session, HashSet<DiscordRole> roles)
    {
        foreach (var role in roles)
        {
            if (HasRole(session.UserId, role))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Loads the roles of a player who is still connecting, so connection checks can read them before the
    /// session exists. The session load replaces them.
    /// </summary>
    public async Task Preload(NetUserId player, CancellationToken cancel = default)
    {
        var roleIds = await _db.GetDiscordRoleIds(player, cancel);
        if (_connecting.Count >= MaxConnecting)
            _connecting.Clear();

        _connecting[player] = _roleIds.Resolve(roleIds);
    }

    public async Task<bool> HasRole(NetUserId player, DiscordRole role, CancellationToken cancel)
    {
        return _roleIds.TryGetId(role, out var roleId) &&
               await _db.HasDiscordRole(player, roleId, cancel);
    }

    public async Task ReloadRoles(ICommonSession player, CancellationToken cancel = default)
    {
        await LoadData(player, cancel);
        SendRoles(player);
    }

    public void SetRoleOverride(NetUserId player, DiscordRole role, bool enabled)
    {
        if (!_roleOverrides.TryGetValue(player, out var roles))
        {
            if (!enabled)
                return;

            roles = [];
            _roleOverrides.Add(player, roles);
        }

        if (enabled)
            roles.Add(role);
        else
            roles.Remove(role);

        if (roles.Count == 0)
            _roleOverrides.Remove(player);

        if (_player.TryGetSessionById(player, out var session))
            SendRoles(session);

        RolesUpdated?.Invoke();
    }

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var roleIds = await _db.GetDiscordRoleIds(player.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        _roles[player.UserId] = _roleIds.Resolve(roleIds);
        _connecting.Remove(player.UserId);
        RolesUpdated?.Invoke();
    }

    private void FinishLoad(ICommonSession player)
    {
        SendRoles(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _roles.Remove(player.UserId);
        _connecting.Remove(player.UserId);
        _roleOverrides.Remove(player.UserId);
    }

    private void SendRoles(ICommonSession player)
    {
        var roles = _roles.GetValueOrDefault(player.UserId) is { } cached
            ? new HashSet<DiscordRole>(cached)
            : [];
        if (_roleOverrides.TryGetValue(player.UserId, out var overrides))
            roles.UnionWith(overrides);
        _net.ServerSendMessage(new DiscordRolesStatusMsg { Roles = roles }, player.Channel);
    }

    private void OnDiscordRolesUpdated(DatabaseNotification notification)
    {
        if (notification.Channel != DiscordRoleConstants.UpdatedNotificationChannel ||
            notification.Payload == null ||
            !Guid.TryParse(notification.Payload, out var playerId))
        {
            return;
        }

        _task.RunOnMainThread(() => ReloadPlayerRoles(playerId));
    }

    private async void ReloadPlayerRoles(Guid playerId)
    {
        if (_player.TryGetSessionById(new NetUserId(playerId), out var session))
            await ReloadRoles(session);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordRolesStatusMsg>();
        _db.SubscribeToNotifications(OnDiscordRolesUpdated);
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
