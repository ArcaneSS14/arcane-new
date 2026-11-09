using Content.Arcane.Common.Camera;
using Content.Shared.Camera;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{

    private static readonly ArcaneScreenshakeParameters ArcaneMeleeUserShake = new(0.08f, 1.0f, 0.009f);
    private static readonly ArcaneScreenshakeParameters ArcaneMeleeTargetShake = new(0.45f, 1.1f, 0.04f);

    private void DoArcaneMeleeScreenshake(EntityUid user, HashSet<EntityUid> targets)
    {
        ArcaneScreenshakeRequest.Raise(EntityManager, user, null, ArcaneMeleeUserShake);
        foreach (var target in targets)
            ArcaneScreenshakeRequest.Raise(EntityManager, target, ArcaneMeleeTargetShake, null);
    }
}
