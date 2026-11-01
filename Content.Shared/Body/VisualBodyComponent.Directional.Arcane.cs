using Robust.Shared.GameStates;

namespace Content.Shared.Body;

public sealed partial class VisualBodyComponent
{
    [DataField, AutoNetworkedField]
    public bool EarsAboveHair;
}
