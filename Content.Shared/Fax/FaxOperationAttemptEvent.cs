// Arcane-Start
namespace Content.Shared.Fax;

/// <summary>Raised on the inserted item before a fax sends or copies it.</summary>
[ByRefEvent]
public record struct FaxOperationAttemptEvent(EntityUid Fax, EntityUid Actor, bool Copy, bool Handled = false);
// Arcane-End
