namespace Content.Arcane.Shared.Tools;

/// <summary>Weld seam in the target's local coordinate frame.</summary>
[RegisterComponent]
public sealed partial class WeldingEffectPathComponent : Component
{
    [DataField]
    public Vector2 StartOffset;

    [DataField]
    public Vector2 EndOffset;
}
