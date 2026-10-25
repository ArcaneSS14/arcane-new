namespace Content.Arcane.Shared.Medical.Heart;

/// <summary>
/// Configures whether the body's circulation needs a functioning heart.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HeartCirculationComponent : Component
{
    [DataField]
    public bool RequiresHeart = true;
}
