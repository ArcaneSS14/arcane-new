using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Posing;

/// <summary>
/// Lets a mob freeze in place and shift or tilt its sprite, e.g. for photos.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PosingComponent : Component
{
    /// <summary>
    /// Sprite shift in tiles on top of <see cref="DefaultOffset"/>, reset by every toggle.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    /// <summary>
    /// Sprite rotation, reset by every toggle.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Angle CurrentAngle = Angle.Zero;

    /// <summary>
    /// Largest <see cref="CurrentOffset"/> on each axis in either direction, in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 OffsetLimits = new(0.3f, 0.3f);

    /// <summary>
    /// Largest <see cref="CurrentAngle"/> in either direction, in degrees.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AngleLimits = 180f;

    /// <summary>
    /// While true the mob cannot move, and its local player uses the posing key bindings.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Posing;

    /// <summary>
    /// Sprite offset of the mob when it does not pose, restored when posing ends.
    /// </summary>
    [DataField]
    public Vector2 DefaultOffset = Vector2.Zero;

    /// <summary>
    /// Sprite rotation of the mob when it does not pose, in degrees, restored when posing ends.
    /// </summary>
    [DataField]
    public float DefaultAngle;
}
