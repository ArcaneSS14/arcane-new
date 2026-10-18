namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Applies completed compressions before the inherited revival roll and assisted breath.
/// </summary>
[ByRefEvent]
public struct CPRCompletedEvent(EntityUid user, bool canVentilate, float reviveChance)
{
    public readonly EntityUid User = user;
    public readonly bool CanVentilate = canVentilate;
    public float ReviveChance = reviveChance;
    public bool CanRevive = true;
}
