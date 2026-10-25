// Arcane-Start
using Content.Shared.Camera;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    [Dependency] private GameTicker _arcaneTicker = default!;
    [Dependency] private ArcaneScreenshakeSystem _arcaneScreenshake = default!;

    private static readonly ArcaneScreenshakeParameters ArcaneGridShake = new(0.8f, 0.04f, 0.015f);

    private bool TryArcaneScreenshakeGrid(EntityUid uid)
    {
        if (Timing.CurTime - _arcaneTicker.RoundStartTimeSpan < TimeSpan.FromSeconds(30))
            return true;

        _arcaneScreenshake.Screenshake(Filter.BroadcastGrid(uid), ArcaneGridShake, null);
        return true;
    }
}
// Arcane-End
