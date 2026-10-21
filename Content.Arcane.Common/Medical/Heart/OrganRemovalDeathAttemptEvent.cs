namespace Content.Arcane.Common.Medical.Heart;

/// <summary>
/// Raised on a removed organ before applying the default delayed death behavior.
/// A replacement organ simulation may cancel that behavior.
/// </summary>
[ByRefEvent]
public struct OrganRemovalDeathAttemptEvent
{
    public bool Cancelled;
}
