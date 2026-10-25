using Content.Arcane.Common.Medical.Wounds;
using Content.Medical.Shared.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.Arcane.Shared.Medical.Wounds;

public sealed partial class WoundTreatmentSystem : EntitySystem
{
    [Dependency] private WoundSystem _wounds = default!;
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WoundableComponent, MedicalHealingEvent>(OnHealing);
    }

    public DamageSpecifier GetDamage(Entity<WoundableComponent> part, DamageSpecifier? damage = null)
    {
        damage ??= new DamageSpecifier();
        foreach (var wound in _wounds.GetWoundableWounds(part.AsNullable()))
        {
            var type = wound.Comp.DamageType;
            damage.DamageDict[type] = damage.DamageDict.GetValueOrDefault(type) + wound.Comp.WoundSeverityPoint;
        }

        return damage;
    }

    private void OnHealing(Entity<WoundableComponent> ent, ref MedicalHealingEvent args)
    {
        var before = GetDamage(ent);
        var raw = _damage.ChangeDamage(ent.Owner, args.Damage, true, origin: args.User,
            ignoreBlockers: args.IgnoreBlockers);
        var after = GetDamage(ent);
        var consumed = new DamageSpecifier(raw);
        foreach (var (type, amount) in before.DamageDict)
        {
            var change = after.DamageDict.GetValueOrDefault(type) - amount;
            if (change < 0)
                consumed.DamageDict[type] = FixedPoint2.Min(consumed.DamageDict.GetValueOrDefault(type), change);
        }

        // The same dose heals both representations; count their maximum, not their sum.
        args.Consumed = consumed;
    }
}
