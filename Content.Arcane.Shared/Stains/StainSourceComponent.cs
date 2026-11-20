using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Stains;

/// <summary>
/// Stepping into this (a puddle) stains the shoes of whoever steps in it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StainSourceComponent : Component;
