// Arcane-Start
namespace Content.Shared.Body.Components;

/// <summary>
/// Arcane - brains are named after their owner once removed (Orion #252).
/// </summary>
public sealed partial class BrainComponent
{
    [DataField, AutoNetworkedField]
    public bool Renamed;
}
// Arcane-End
