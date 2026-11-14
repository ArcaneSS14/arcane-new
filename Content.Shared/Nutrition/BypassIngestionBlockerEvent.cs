namespace Content.Shared.Nutrition;

/// <summary>Raised on an item before it blocks oral intake.</summary>
[ByRefEvent]
public record struct BypassIngestionBlockerEvent(bool Bypass = false);
