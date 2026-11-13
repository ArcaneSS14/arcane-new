using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.HeatTint;

[RegisterComponent]
public sealed partial class HeatTintComponent : Component
{
    /// <summary>Gradient stops in increasing temperature, in Kelvin.</summary>
    [DataField(required: true)]
    public List<HeatTintPoint> ColorGradient = new();

    [DataField]
    public List<string>? AffectedLayers;

    /// <summary>The relevant solution; changes to other solutions do not recolor this item.</summary>
    [DataField]
    public string Solution = "food";

    // Client presentation state belongs to each component instance; -1 denotes the entire sprite.
    public readonly Dictionary<int, Color> BaseColors = new();
    public readonly Dictionary<int, Color> LastAppliedColors = new();
}

[DataDefinition]
public sealed partial class HeatTintPoint
{
    [DataField(required: true)]
    public float Temperature;

    [DataField(required: true)]
    public Color Color = Color.White;
}

[Serializable, NetSerializable]
public enum HeatTintVisuals : byte
{
    Temperature,
}
