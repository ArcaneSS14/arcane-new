namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Selects whether the performer can provide rescue breaths or only chest compressions.
/// </summary>
[ByRefEvent]
public struct CPRCapabilitiesEvent(EntityUid user)
{
    public readonly EntityUid User = user;
    public bool Handled;
    public bool CanPerform;
    public bool CanVentilate = true;
    public string? VerbText;
}
