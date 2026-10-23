namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Notifies circulation systems after CPR has successfully brought a patient out of death.
/// </summary>
[ByRefEvent]
public readonly record struct CPRRevivedEvent(EntityUid User);
