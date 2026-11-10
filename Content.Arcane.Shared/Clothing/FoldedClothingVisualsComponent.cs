namespace Content.Arcane.Shared.Clothing;

/// <summary>
/// A separate RSI containing compatible folded worn states for an existing uniform.
/// Unfolded, icon and in-hand visuals remain owned by the original clothing prototype.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FoldedClothingVisualsComponent : Component
{
    /// <summary>
    /// False on variants whose existing artwork has no compatible folded states.
    /// The corresponding Foldable component is also disabled in the prototype.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Folded-only RSI, relative to Textures. Null supplies no visual override.
    /// </summary>
    [DataField("sprite")]
    public string? RsiPath;
}
