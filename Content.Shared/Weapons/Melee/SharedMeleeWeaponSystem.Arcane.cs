// Arcane-Start
using Content.Shared.Camera;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private ArcaneScreenshakeSystem _arcaneScreenshake = default!;

    private static readonly ArcaneScreenshakeParameters ArcaneMeleeUserShake = new(0.08f, 1.0f, 0.009f);
    private static readonly ArcaneScreenshakeParameters ArcaneMeleeTargetShake = new(0.45f, 1.1f, 0.04f);

    private void DoArcaneMeleeScreenshake(EntityUid user, List<EntityUid> targets)
    {
        _arcaneScreenshake.Screenshake(user, null, ArcaneMeleeUserShake);
        foreach (var target in targets)
            _arcaneScreenshake.Screenshake(target, ArcaneMeleeTargetShake, null);
    }
}
// Arcane-End
