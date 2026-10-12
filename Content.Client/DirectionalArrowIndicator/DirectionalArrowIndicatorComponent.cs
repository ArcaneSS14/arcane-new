using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.DirectionalArrowIndicator;

/// <summary>
/// Component that manages directional arrow indicators that spawn clientside when an entity is examined.
/// Handled by <see cref="DirectionalArrowIndicatorSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class DirectionalArrowIndicatorComponent : Component
{
    // Arcane-Start
    /// <summary>
    /// Optional boolean appearance key that mirrors the arrows along their local Y axis.
    /// </summary>
    [DataField]
    public Enum? FlipOnAppearance;

    /// <summary>
    /// Client-only arrows from the last examination.
    /// </summary>
    public List<EntityUid> SpawnedArrows = new();
    // Arcane-End

    /// <summary>
    /// The lifetime of the arrow indicator in seconds.
    /// </summary>
    [DataField]
    public float Lifetime = 2f;

    /// <summary>
    /// List of arrows to spawn.
    /// </summary>
    [DataField]
    public List<ArrowSpawnData> Arrows = new();
}

/// <summary>
/// Data for spawning a single directional arrow.
/// </summary>
[DataDefinition]
public sealed partial class ArrowSpawnData
{
    /// <summary>
    /// Offset relative to entity's position where the arrow should appear.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Rotation of the arrow.
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// Prototype ID of the arrow entity to spawn.
    /// </summary>
    [DataField]
    public EntProtoId ArrowType = "RedDirectionalArrowIndicator";
}
