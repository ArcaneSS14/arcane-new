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
    private readonly Dictionary<NetUserId, RoleLoadState> _roleLoads = new();
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
        var state = AcquireRoleLoad(player);
        var entered = false;
        try
        {
            await state.Gate.WaitAsync(cancel);
            entered = true;
            if (!IsCurrentRoleLoad(player, state))
                return;

            var roleIds = await _db.GetDiscordRoleIds(player, cancel);
            cancel.ThrowIfCancellationRequested();
            if (!IsCurrentRoleLoad(player, state))
                return;

            if (_connecting.Count >= MaxConnecting)
                _connecting.Clear();

            _connecting[player] = _roleIds.Resolve(roleIds);
        }
        finally
        {
            if (entered)
                state.Gate.Release();
            ReleaseRoleLoad(player, state);
        }
    }

    public async Task<bool> HasRole(NetUserId player, DiscordRole role, CancellationToken cancel)
    {
        return _roleIds.TryGetId(role, out var roleId) &&
               await _db.HasDiscordRole(player, roleId, cancel);
    }

    public async Task ReloadRoles(ICommonSession player, CancellationToken cancel = default)
    {
        if (await TryLoadData(player, cancel)
            && _player.TryGetSessionById(player.UserId, out var current)
            && ReferenceEquals(current, player))
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
        await TryLoadData(player, cancel);
    }

    private async Task<bool> TryLoadData(ICommonSession player, CancellationToken cancel)
    {
        var userId = player.UserId;
        var state = AcquireRoleLoad(userId);
        var entered = false;
        try
        {
            await state.Gate.WaitAsync(cancel);
            entered = true;
            if (!IsCurrentRoleLoad(userId, state))
                return false;

            var roleIds = await _db.GetDiscordRoleIds(userId, cancel);
            cancel.ThrowIfCancellationRequested();
            if (!IsCurrentRoleLoad(userId, state)
                || !_player.TryGetSessionById(userId, out var current)
                || !ReferenceEquals(current, player))
                return false;

            _roles[userId] = _roleIds.Resolve(roleIds);
            _connecting.Remove(userId);
            RolesUpdated?.Invoke();
            return true;
        }
        finally
        {
            if (entered)
                state.Gate.Release();
            ReleaseRoleLoad(userId, state);
        }
    }

    private RoleLoadState AcquireRoleLoad(NetUserId userId)
    {
        if (!_roleLoads.TryGetValue(userId, out var state))
            _roleLoads[userId] = state = new RoleLoadState();
        state.Pending++;
        return state;
    }

    private bool IsCurrentRoleLoad(NetUserId userId, RoleLoadState state)
    {
        return _roleLoads.TryGetValue(userId, out var current) && ReferenceEquals(current, state);
    }

    private void ReleaseRoleLoad(NetUserId userId, RoleLoadState state)
    {
        state.Pending--;
        if (state.Pending != 0)
            return;

        if (IsCurrentRoleLoad(userId, state))
            _roleLoads.Remove(userId);
        state.Gate.Dispose();
    }

    private sealed class RoleLoadState
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int Pending;
    }

    private void FinishLoad(ICommonSession player)
    {
        SendRoles(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _roleLoads.Remove(player.UserId);
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
