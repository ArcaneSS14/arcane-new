namespace Content.Arcane.Common.Medical.Concussion;

/// <summary>
/// Carries the concussion dose after flashbang range and hearing protection have been resolved.
/// </summary>
[ByRefEvent]
public readonly record struct FlashbangConcussionEvent(float Damage);
