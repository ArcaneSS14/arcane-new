using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Sprite;

/// <summary>
/// Visually connects anchored entities to adjacent ones with the same <see cref="ConnectionKey"/>,
/// e.g. computer consoles placed side by side.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EdgeConnectionComponent : Component
{
    /// <summary>
    /// Only entities with equal keys connect.
    /// </summary>
    [DataField]
    public string ConnectionKey = "default";

    /// <summary>
    /// Directions this entity can connect in, relative to its rotation.
    /// </summary>
    [DataField]
    public EdgeConnectionDirections AllowedDirections = EdgeConnectionDirections.None;

    /// <summary>
    /// Sprite layer swapped by the client when connected.
    /// </summary>
    [DataField]
    public string Layer = "computerLayerBody";

    /// <summary>
    /// Layer states that have connected variants, mapped to a state template where <c>{0}</c> is
    /// replaced with <c>l</c>, <c>r</c> or <c>l_r</c>. Layers in any other state are left alone,
    /// so children with their own sprites are not affected.
    /// </summary>
    [DataField]
    public Dictionary<string, string> ConnectedStates = new();

    /// <summary>
    /// RSI containing the connected variants.
    /// </summary>
    [DataField]
    public ResPath? ConnectedSprite;
}

[Serializable, NetSerializable]
public enum EdgeConnectionVisuals : byte
{
    ConnectionMask,
}

[Flags]
[Serializable, NetSerializable]
public enum EdgeConnectionDirections : byte
{
    None = 0,
    North = 1 << 0,
    South = 1 << 1,
    East = 1 << 2,
    West = 1 << 3,
}
