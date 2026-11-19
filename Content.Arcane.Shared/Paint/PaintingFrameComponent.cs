using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintingFrameComponent : Component
{
    [DataField(required: true)]
    public string Material = string.Empty;
}
