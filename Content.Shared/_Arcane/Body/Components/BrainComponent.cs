namespace Content.Shared.Body.Components;

/// <summary>
/// Brains are named after their owner once removed (Orion #252).
/// </summary>
public sealed partial class BrainComponent
{
    [DataField, AutoNetworkedField]
    public bool Renamed;
}
