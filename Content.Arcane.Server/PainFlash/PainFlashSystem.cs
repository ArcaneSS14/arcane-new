using Content.Arcane.Shared.PainFlash;
using Content.Shared.Damage.Systems;

namespace Content.Arcane.Server.PainFlash;

public sealed class PainFlashSystem : SharedPainFlashSystem
{
    protected override void OnDamageChanged(Entity<PainFlashComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryGetPain(args, out var damage))
            return;

        RaiseNetworkEvent(new PainFlashMessage(GetNetEntity(ent.Owner), damage, Timing.CurTick), ent);
    }
}
