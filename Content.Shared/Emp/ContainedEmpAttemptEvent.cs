namespace Content.Shared.Emp;

/// <summary>
/// Raised on each containing entity before an EMP affects an item or occupant inside it.
/// </summary>
[ByRefEvent]
public record struct ContainedEmpAttemptEvent(EntityUid Target, bool Cancelled = false);
