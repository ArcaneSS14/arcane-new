namespace Content.Arcane.Shared.Tools;

/// <summary>Replicated presentation for one active welding action.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeldingEffectComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan StartTime;

    [AutoNetworkedField]
    public TimeSpan Duration;

    [AutoNetworkedField]
    public Vector2 StartOffset;

    [AutoNetworkedField]
    public Vector2 EndOffset;

    [AutoNetworkedField]
    public bool Experimental;
}
