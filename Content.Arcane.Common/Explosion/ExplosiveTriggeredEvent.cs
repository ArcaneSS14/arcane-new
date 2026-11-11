namespace Content.Arcane.Common.Explosion;

/// <summary>
/// Raised on an explosive after its server-authoritative explosion has been queued.
/// </summary>
[ByRefEvent]
public readonly record struct ExplosiveTriggeredEvent(EntityUid? User = null);
