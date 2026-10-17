namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Scales metabolizable inhaled gas into the brain's oxygen reserve.
/// </summary>
[RegisterComponent]
public sealed partial class LungOxygenComponent : Component
{
    [DataField]
    public float AirSaturationGain = 0.01f;
}
