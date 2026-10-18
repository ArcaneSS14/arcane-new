using Content.Arcane.Common.Medical.Respiration;
using Content.Server.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Adjusts oxygen loss and recovery at the existing respiratory cadence, retaining body-specific damage modifiers.
/// </summary>
public sealed partial class RespirationBalanceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RespiratorComponent, RespirationDamageEvent>(OnDamage);
    }

    private void OnDamage(Entity<RespiratorComponent> ent, ref RespirationDamageEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue("Asphyxiation", out var amount))
            return;

        var critical = TryComp<MobStateComponent>(ent, out var mob) && mob.CurrentState is MobState.SoftCrit or MobState.Critical;
        var rate = args.Suffocating ? critical ? 0.33f : 1.5f : critical ? 0f : 0.625f;
        var damage = new DamageSpecifier(args.Damage);
        damage.DamageDict["Asphyxiation"] = amount * (rate * (float) args.Interval.TotalSeconds);
        args.Damage = damage;
    }
}
