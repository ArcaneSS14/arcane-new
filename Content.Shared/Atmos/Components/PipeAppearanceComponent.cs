using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeAppearanceComponent : Component
{
    // Arcane-Start: Exposed machine ports must update without SubFloorHide.
    [DataField]
    public bool ExposedConnections;
    // Arcane-End

    [DataField]
    public SpriteSpecifier.Rsi[] Sprite = [new(new("Structures/Piping/Atmospherics/pipe.rsi"), "pipeConnector"),
        new(new("Structures/Piping/Atmospherics/pipe_alt1.rsi"), "pipeConnector"),
        new(new("Structures/Piping/Atmospherics/pipe_alt2.rsi"), "pipeConnector")];
}
