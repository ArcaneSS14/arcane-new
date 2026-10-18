using Content.Arcane.Common.Medical.Respiration;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Arcane.Server.Medical.Respiration;

public sealed partial class CPRRecoverySystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, CPRCompletedEvent>(OnCPRCompleted);
    }

    private void OnCPRCompleted(Entity<DamageableComponent> ent, ref CPRCompletedEvent args)
    {
        _damage.ChangeDamage(ent.Owner,
            new DamageSpecifier { DamageDict = { ["Asphyxiation"] = -3 } },
            targetPart: TargetBodyPart.Vital, interruptsDoAfters: false, ignoreResistances: true);
    }
}
