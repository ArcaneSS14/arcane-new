namespace Content.Shared.Cuffs;

/// <summary>
/// Raised on the user to modify the time needed to attach a restraint.
/// </summary>
[ByRefEvent]
public record struct ModifyCuffDurationEvent(EntityUid Target, float Duration);
