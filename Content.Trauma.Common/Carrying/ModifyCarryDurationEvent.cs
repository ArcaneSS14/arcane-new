namespace Content.Trauma.Common.Carrying;

/// <summary>
/// Raised on the carrier to modify the delay before picking someone up.
/// </summary>
[ByRefEvent]
public record struct ModifyCarryDurationEvent(EntityUid Carried, float Multiplier = 1f);
