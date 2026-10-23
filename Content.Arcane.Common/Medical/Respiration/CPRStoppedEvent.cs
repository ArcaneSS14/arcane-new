namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Ends the circulatory assistance owned by a CPR session.
/// </summary>
[ByRefEvent]
public readonly record struct CPRStoppedEvent;
