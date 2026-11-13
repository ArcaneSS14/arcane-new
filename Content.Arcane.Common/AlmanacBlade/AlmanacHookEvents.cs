namespace Content.Arcane.Common.AlmanacBlade;

[ByRefEvent]
public readonly record struct AlmanacFlipPerformedEvent;

[ByRefEvent]
public record struct CargoProductAvailabilityEvent(string Product, bool Hidden = false);
