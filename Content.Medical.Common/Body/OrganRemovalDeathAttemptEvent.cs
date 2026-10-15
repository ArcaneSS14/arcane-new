// Arcane-Start
namespace Content.Medical.Common.Body;

/// <summary>
/// Raised on a removed organ before applying the default delayed death behavior.
/// A replacement organ simulation may cancel that behavior.
/// </summary>
[ByRefEvent]
public struct OrganRemovalDeathAttemptEvent
{
    public bool Cancelled;
}
// Arcane-End
