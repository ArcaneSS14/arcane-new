using Content.Shared.Explosion.EntitySystems;
using Robust.Client.Graphics;

namespace Content.Arcane.Client.Explosion;

public sealed partial class ExplosionEffectSystem : SharedExplosionEffectSystem
{
    [Dependency] private IOverlayManager _overlays = default!;

    private ExplosionShockWaveOverlay _shockWave = default!;

    public override void Initialize()
    {
        base.Initialize();
        _shockWave = new ExplosionShockWaveOverlay();
        _overlays.AddOverlay(_shockWave);
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay(_shockWave);
        _shockWave.Dispose();
        base.Shutdown();
    }
}
