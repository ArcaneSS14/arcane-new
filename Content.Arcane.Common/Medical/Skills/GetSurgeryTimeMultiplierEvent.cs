namespace Content.Arcane.Common.Medical.Skills;

/// <summary>
/// Raised on the surgeon to get the multiplier for how long a surgery step takes.
/// </summary>
[ByRefEvent]
public record struct GetSurgeryTimeMultiplierEvent(float Multiplier);
