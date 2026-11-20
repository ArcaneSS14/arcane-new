using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintEaselComponent : Component
{
    [DataField]
    public string Slot = "canvas";

    [DataField]
    public float CanvasScale = 0.75f;
}
