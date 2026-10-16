using Content.Server.Power.Generation.Teg;
using Content.Shared.Construction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Repairable;

namespace Content.Arcane.Server.Construction.Conditions;

/// <summary>
/// Prevents dismantling TEG parts with more than 25 damage that their repair tool can heal.
/// </summary>
[DataDefinition]
public sealed partial class TegRepairableDamage : IGraphCondition
{
    private const int MaximumDamage = 25;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.HasComponent<TegGeneratorComponent>(uid) &&
            !entityManager.HasComponent<TegCirculatorComponent>(uid))
            return true;

        if (!entityManager.TryGetComponent(uid, out RepairableComponent? repairable) ||
            !entityManager.TryGetComponent(uid, out DamageableComponent? damageable))
            return true;

        var damageSystem = entityManager.System<DamageableSystem>();
        if (repairable.DamageValue is { } value)
            return value >= 0 || damageSystem.GetTotalDamage((uid, damageable)) <= MaximumDamage;

        if (repairable.Damage == null)
            return damageSystem.GetTotalDamage((uid, damageable)) <= MaximumDamage;

        var currentDamage = damageSystem.GetAllDamage((uid, damageable));
        var repairableDamage = FixedPoint2.Zero;
        foreach (var (type, amount) in repairable.Damage.DamageDict)
        {
            if (amount < 0 && currentDamage.DamageDict.TryGetValue(type, out var damage))
                repairableDamage += damage;
        }

        return repairableDamage <= MaximumDamage;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (Condition(args.Examined, IoCManager.Resolve<IEntityManager>()))
            return false;

        args.PushMarkup(Loc.GetString("teg-deconstruction-repair-required"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield break;
    }
}
