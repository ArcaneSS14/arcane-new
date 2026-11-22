// Arcane-Start
using Robust.Shared.GameObjects;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Directional intent before relay and container handling, retaining the original input source.
/// </summary>
[ByRefEvent]
public record struct DirectionInputEvent(EntityUid Source, Direction Direction, bool State);
// Arcane-End
