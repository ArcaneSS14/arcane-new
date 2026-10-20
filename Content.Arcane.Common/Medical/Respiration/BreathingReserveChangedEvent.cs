namespace Content.Arcane.Common.Medical.Respiration;

[ByRefEvent]
public readonly record struct BreathingReserveChangedEvent(bool HasReserve);
