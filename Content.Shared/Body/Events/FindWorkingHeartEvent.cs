// Arcane-Start
namespace Content.Shared.Body.Events;

/// <summary>
/// Queries circulation before bloodstream damage is applied. A listener can suppress
/// missing-heart damage without changing the actual blood volume, for example in stasis.
/// </summary>
[ByRefEvent]
public struct FindWorkingHeartEvent()
{
    public bool Found;
    public bool DoEffects = true;
}
// Arcane-End
