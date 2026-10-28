using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Posing;

/// <summary>
/// Lets a mob freeze in place and shift or tilt its sprite, e.g. for photos.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PosingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    [ViewVariables, AutoNetworkedField]
    public Angle CurrentAngle = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLimits = new(0.3f, 0.3f);

    [DataField, AutoNetworkedField]
    public float AngleLimits = 180f;

    [ViewVariables, AutoNetworkedField]
    public bool Posing;

    [DataField]
    public string DefaultInputContext = "human";

    [DataField]
    public Vector2 DefaultOffset = Vector2.Zero;

    [DataField]
    public float DefaultAngle;
}
