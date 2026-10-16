namespace Content.Arcane.Common.Medical.Heart;

/// <summary>
/// Queries circulation before bloodstream damage is applied. A listener opts into
/// missing-heart damage and can suppress it, for example while the patient is in stasis.
/// </summary>
[ByRefEvent]
public struct FindWorkingHeartEvent
{
    public bool Found;
    public bool DoEffects;
}
