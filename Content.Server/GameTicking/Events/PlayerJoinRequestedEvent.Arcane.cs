// Arcane-Start
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Events;

/// <summary>
/// Content may defer entry after connection checks, before player data starts loading.
/// </summary>
[ByRefEvent]
public struct PlayerJoinRequestedEvent(ICommonSession session)
{
    public readonly ICommonSession Session = session;
    public bool Deferred;
}
// Arcane-End
