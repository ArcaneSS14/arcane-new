using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Shared.Medical.Wounds;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;

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
            if (TryComp<WoundableComponent>(selected, out var woundable) && _body.GetBody(selected) == body)
                args.WoundDamage = _wounds.GetDamage((selected, woundable));
            return;
        }

        // Parts are stored directly on the body in the current anatomy model.
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
            args.WoundDamage = _wounds.GetDamage(part, args.WoundDamage);
    }
}
