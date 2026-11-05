// Arcane-Start
using System.Numerics;
using Content.Shared.Camera;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    [Dependency] private ArcaneScreenshakeSystem _arcaneScreenshake = default!;

    private static readonly ArcaneScreenshakeParameters ArcaneSmallExplosionShake = new(0.4f, 0.2f, 0.014f);
    private static readonly ArcaneScreenshakeParameters ArcaneLargeExplosionShake = new(0.6f, 0.05f, 0.014f);

    private void DoArcaneExplosionScreenshake(Filter filter, MapCoordinates epicenter, float range, bool small)
    {
        if (!float.IsFinite(range) || range <= 0f)
            return;

        var parameters = small ? ArcaneSmallExplosionShake : ArcaneLargeExplosionShake;

        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity is not { } uid)
                continue;

            var distance = Vector2.Distance(epicenter.Position, _transformSystem.GetWorldPosition(uid));
            if (!float.IsFinite(distance))
                continue;

            var attenuation = Math.Clamp(1f - distance / range, 0f, 1f);
            if (attenuation <= 0f)
                continue;

            _arcaneScreenshake.Screenshake(uid,
                new ArcaneScreenshakeParameters(parameters.Trauma * attenuation, parameters.DecayRate, parameters.Frequency),
                null);
        }
    }
}
// Arcane-End
