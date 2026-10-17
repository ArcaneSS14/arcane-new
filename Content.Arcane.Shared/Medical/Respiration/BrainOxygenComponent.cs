namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Stores the brain's oxygen reserve independently of the body's breathing cycle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BrainOxygenComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AirSaturation = 1f;

    [ViewVariables, AutoNetworkedField]
    public bool Active;

    [DataField]
    public float AirConsumption = 0.02f;

    [DataField]
    public float AsphyxiationDamage = 8f;

    [DataField]
    public float CPRRecovery = 3f;

    [ViewVariables]
    public EntityUid? LastBody;

    [ViewVariables]
    public BrainOxygenLevel? LastLevel;

    [ViewVariables]
    public bool HadVisualEffect;
}

public enum BrainOxygenLevel : byte
{
    Stable,
    Unstable,
    Dangerous,
    Critical,
    Fatal,
}
