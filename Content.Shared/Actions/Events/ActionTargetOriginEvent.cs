// Arcane-Start
namespace Content.Shared.Actions.Events;

/// <summary>
/// Resolves the physical origin of an action without changing its operator or bypassing target checks.
/// </summary>
[ByRefEvent]
public record struct ActionTargetOriginEvent(EntityUid Action, EntityUid Origin);
// Arcane-End
