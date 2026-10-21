using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Shares the body's oxygen knockout threshold between resuscitation and respiratory symptoms.
/// </summary>
public sealed partial class OxygenThresholdSystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _thresholds = default!;

    public float? GetThreshold(EntityUid patient)
    {
        if (!_thresholds.TryGetThresholdForState(patient, MobState.SoftCrit, out var threshold) &&
            !_thresholds.TryGetThresholdForState(patient, MobState.Critical, out threshold))
            return null;

        return threshold.Value > 0 ? threshold.Value.Float() / 2f : null;
    }
}
