using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.PDA;

/// <summary>
/// Shows what the PDA is doing on its screen: off, the menu or the active program.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PdaScreenComponent : Component
{
    /// <summary>
    /// Screen state per program prototype.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, string> ProgramScreens = new();

    [DataField]
    public string OffState = "pda_screen_borders";

    [DataField]
    public string MenuState = "menu";

    /// <summary>
    /// Shown for programs without their own screen.
    /// </summary>
    [DataField]
    public string ProgramState = "generic";
}

[Serializable, NetSerializable]
public enum PdaScreenVisuals : byte
{
    State,
}

public enum PdaScreenLayers : byte
{
    Screen,
}
