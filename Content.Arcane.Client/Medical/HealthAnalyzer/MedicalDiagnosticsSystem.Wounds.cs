using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Shared.Medical.Wounds;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;

namespace Content.Arcane.Client.Medical.HealthAnalyzer;

public sealed partial class MedicalDiagnosticsSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundTreatmentSystem _wounds = default!;

    private void PopulateWounds(EntityUid body, ref HealthAnalyzerPopulateEvent args)
    {
        if (!args.IncludeWounds)
            return;

        if (args.Part is { } selected)
        {
            if (_body.GetBody(selected) == body)
                args.DiagnosticDamage = GetDiagnosticDamage(selected);
            return;
        }

        var damage = new DamageSpecifier();
        foreach (var part in _body.GetExternalOrgans(body))
            damage += GetDiagnosticDamage(part);
        args.DiagnosticDamage = damage;
    }

    private DamageSpecifier GetDiagnosticDamage(EntityUid part)
    {
        var damage = _damage.GetAllDamage(part);
        if (!TryComp<WoundableComponent>(part, out var woundable))
            return damage;

        // Wounds and raw damage can represent the same injury or outlive one another.
        foreach (var (type, amount) in _wounds.GetDamage((part, woundable)).DamageDict)
        {
            if (amount > damage.DamageDict.GetValueOrDefault(type))
                damage.DamageDict[type] = amount;
        }
        return damage;
    }
}
