namespace Content.Arcane.Shared.Weapons.Ranged;

/// <summary>
/// Draws a fading line behind a moving projectile on the client.
/// </summary>
[RegisterComponent]
public sealed partial class TracerComponent : Component
{
    /// <summary>
    /// How long the tracer is drawn, in seconds.
    /// </summary>
    [DataField]
    public float Lifetime = 10f;

    /// <summary>
    /// The maximum length of the tracer trail.
    /// </summary>
    [DataField]
    public float Length = 2f;

    /// <summary>
    /// Color of the tracer line.
    /// </summary>
    [DataField]
    public Color Color = Color.Red;

    /// <summary>
    /// World positions the projectile has passed through, oldest first.
    /// </summary>
    [ViewVariables]
    public List<Vector2> PositionHistory = new();

    /// <summary>
    /// Length of the polyline through <see cref="PositionHistory"/>, kept up to date as points come and go.
    /// </summary>
    [ViewVariables]
    public float TrailLength;

    /// <summary>
    /// When the tracer stops being drawn.
    /// </summary>
    [ViewVariables]
    public TimeSpan EndTime;
}
