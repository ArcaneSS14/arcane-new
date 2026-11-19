using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Vision;

/// <summary>
/// Applies the optional UltraVision colour filter to the attached player's view.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UltraVisionComponent : Component;
