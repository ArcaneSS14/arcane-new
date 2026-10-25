// Arcane-Start
using Content.Shared.Camera;
using Robust.Shared.Player;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    [Dependency] private ArcaneScreenshakeSystem _arcaneScreenshake = default!;

    private static readonly ArcaneScreenshakeParameters ArcaneSmallExplosionShake = new(0.4f, 0.2f, 0.014f);
    private static readonly ArcaneScreenshakeParameters ArcaneLargeExplosionShake = new(0.6f, 0.05f, 0.014f);

    private void DoArcaneExplosionScreenshake(Filter filter, bool small)
    {
        _arcaneScreenshake.Screenshake(filter, small ? ArcaneSmallExplosionShake : ArcaneLargeExplosionShake, null);
    }
}
// Arcane-End
