// Arcane-Start
using Content.Shared.Camera;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private ArcaneScreenshakeSystem _arcaneScreenshake = default!;

    private void DoArcaneGunScreenshake(EntityUid user, Entity<GunComponent> gun)
    {
        _arcaneScreenshake.Screenshake(user, null, new ArcaneScreenshakeParameters(0.085f * gun.Comp.CameraRecoilScalarModified, 1.2f, 0.008f));
    }
}
// Arcane-End
