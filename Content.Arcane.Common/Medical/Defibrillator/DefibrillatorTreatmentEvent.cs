namespace Content.Arcane.Common.Medical.Defibrillator;

/// <summary>
/// Applies a delivered shock before healing and the revival check. Only the original patient receives Arcane treatment.
/// </summary>
[ByRefEvent]
public struct DefibrillatorTreatmentEvent(EntityUid defibrillator, EntityUid user, bool isOriginal)
{
    public readonly EntityUid Defibrillator = defibrillator;
    public readonly EntityUid User = user;
    public readonly bool IsOriginal = isOriginal;
    public bool CanRevive = true;
}
