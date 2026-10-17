namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Raised on the patient after the existing CPR training and lung checks succeed.
/// </summary>
[ByRefEvent]
public struct CPRCompletedEvent()
{
    public bool AllowRevival = true;
}
