namespace Content.Arcane.Common.Medical.Skills;

/// <summary>
/// Raised on the user of a healing item to get the multiplier for how long the application takes.
/// </summary>
[ByRefEvent]
public record struct GetHealingTimeMultiplierEvent(float Multiplier);
