using Content.Shared.Chemistry.Components;
using Robust.Shared.Map;

namespace Content.Shared.Fluids;

// Arcane-Start
/// <summary>Raised by authoritative liquid spills so Arcane wall stains can consume the same solution.</summary>
[ByRefEvent]
public readonly record struct SplashOnWallEvent(EntityCoordinates Coordinates, Solution Solution);
// Arcane-End
