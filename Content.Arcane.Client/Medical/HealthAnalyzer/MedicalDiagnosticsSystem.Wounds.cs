using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;

namespace Content.Arcane.Client.Medical.HealthAnalyzer;

public sealed partial class MedicalDiagnosticsSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wounds = default!;

    private void PopulateWounds(EntityUid body, ref HealthAnalyzerPopulateEvent args)
    {
        if (!args.IncludeWounds)
            return;

        if (args.Part is { } selected)
        {
            if (TryComp<WoundableComponent>(selected, out var woundable) && _body.GetBody(selected) == body)
                args.WoundDamage = CollectWoundDamage((selected, woundable), new DamageSpecifier());
            return;
        }

        // Parts are stored directly on the body in the current anatomy model.
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
            args.WoundDamage = CollectWoundDamage(part, args.WoundDamage ?? new DamageSpecifier());
    }

    private DamageSpecifier CollectWoundDamage(Entity<WoundableComponent> part, DamageSpecifier damage)
    {
        foreach (var wound in _wounds.GetWoundableWounds(part.AsNullable()))
        {
            var type = wound.Comp.DamageType;
            damage.DamageDict[type] = damage.DamageDict.GetValueOrDefault(type) + wound.Comp.WoundSeverityPoint;
        }

        return damage;
    }
}
