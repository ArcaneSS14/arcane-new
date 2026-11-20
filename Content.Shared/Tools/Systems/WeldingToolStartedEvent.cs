// Arcane-Start
using Content.Shared.DoAfter;
using Robust.Shared.Map;

namespace Content.Shared.Tools.Systems;

/// <summary>Raised on the server after a timed welding action starts.</summary>
[ByRefEvent]
public readonly record struct WeldingToolStartedEvent(
    DoAfterId Action, EntityUid Tool, EntityCoordinates Contact, bool DoorWeld, bool Reverse);
// Arcane-End
