using Content.Shared.Camera;
using Content.Shared.Weapons.Ranged.Components;
using Content.Arcane.Common.Camera;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{

    private void DoArcaneGunScreenshake(EntityUid user, Entity<GunComponent> gun)
    {
        ArcaneScreenshakeRequest.Raise(EntityManager, user, null, new ArcaneScreenshakeParameters(0.085f * gun.Comp.CameraRecoilScalarModified, 1.2f, 0.008f));
    }
}
