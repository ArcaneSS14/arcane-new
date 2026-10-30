using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GhostKick;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Arcane.Server.ServerProtection;

/// <summary>
///     Punishments shared by the server protection systems (Orion #175, #318).
/// </summary>
public sealed partial class ServerProtectionPunishmentSystem : EntitySystem
{
    [Dependency] private AdminSystem _adminSystem = default!;
    [Dependency] private GhostKickManager _ghostKick = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IBanManager _banManager = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerLocator _locator = default!;

    /// <summary>
    ///     Bans a player by user id, with their last known address and HWID. 0 minutes is permanent.
    /// </summary>
    public async void ApplyBan(NetUserId userId, string username, string reason, int minutes = 0)
    {
        var banInfo = new CreateServerBanInfo(reason);
        banInfo.AddUser(userId, username);

        var located = await _locator.LookupIdAsync(userId);
        if (located != null)
        {
            banInfo.AddAddress(located.LastAddress);
            banInfo.AddHWId(located.LastHWId);
        }

        if (minutes > 0)
            banInfo.WithMinutes(minutes);
        banInfo.WithSeverity(NoteSeverity.High);

        _banManager.CreateServerBan(banInfo);
        Log.Info($"{username} ({userId}) was banned: {reason}");
    }

    public void DeAdmin(ICommonSession player, string reason)
    {
        _adminManager.DeAdmin(player);
        Log.Warning($"{player.Name} was de-admined: {reason}");
    }

    public void Kick(ICommonSession player, string reason)
    {
        _ghostKick.DoDisconnect(player.Channel, reason);
        Log.Info($"{player.Name} was kicked: {reason}");
    }

    public void DeleteMessages(ICommonSession player)
    {
        _chat.DeleteMessagesBy(player.UserId);
    }

    public void Erase(ICommonSession player)
    {
        _adminSystem.Erase(player.UserId);
    }

    public void SendAdminAlert(string message)
    {
        _chat.SendAdminAlert(message);
        Log.Info(message);
    }
}
