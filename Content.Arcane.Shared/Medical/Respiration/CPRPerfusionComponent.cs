using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// A completed compression is maintaining circulation until the current CPR session ends.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CPRPerfusionComponent : Component;
