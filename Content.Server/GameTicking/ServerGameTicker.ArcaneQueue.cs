// Arcane-Start
using Content.Server.GameTicking.Events;
using System.Linq;
using System.Threading;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    private int _admittedPlayerCount;
    private int _admittedNonAdminPlayerCount;
    public int AdmittedPlayerCount => Volatile.Read(ref _admittedPlayerCount);
    public int AdmittedNonAdminPlayerCount => Volatile.Read(ref _admittedNonAdminPlayerCount);

    public int GetAdmittedPlayerCount()
    {
        var count = new GetAdmittedPlayerCountEvent(_playerManager.PlayerCount);
        RaiseLocalEvent(ref count);
        var admins = _adminManager.ActiveAdmins.Count(s => s.Status == SessionStatus.InGame);
        Volatile.Write(ref _admittedPlayerCount, count.Count);
        Volatile.Write(ref _admittedNonAdminPlayerCount, Math.Max(0, count.Count - admins));
        return count.Count;
    }
}
// Arcane-End
