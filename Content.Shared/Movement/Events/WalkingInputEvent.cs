// Arcane-Start
namespace Content.Shared.Movement.Events;

/// <summary>
/// Walking intent before relay handling, retaining the original input source.
/// </summary>
[ByRefEvent]
public record struct WalkingInputEvent(EntityUid Source, bool Walking);
// Arcane-End
