using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Bed;

/// <summary>
/// A bed two entities can be buckled to side by side, with room for two bedsheets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DoubleBedComponent : Component
{
    [DataField]
    public Vector2 LeftOffset = new(0f, -0.25f);

    [DataField]
    public Vector2 RightOffset = new(0f, 0.25f);

    [DataField]
    public Vector2 LeftBedsheetOffset = new(0f, 0.5f);

    [DataField]
    public Vector2 RightBedsheetOffset = new(0f, 0f);
}

/// <summary>
/// A bedsheet that covers a whole double bed.
/// </summary>
[RegisterComponent]
public sealed partial class DoubleBedSheetComponent : Component;
