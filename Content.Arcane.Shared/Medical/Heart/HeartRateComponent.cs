using Content.Shared.Alert;

namespace Content.Arcane.Shared.Medical.Heart;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeartRateComponent : Component
{
    /// <summary>
    /// The resting heart rate in beats per minute.
    /// </summary>
    [DataField]
    public float NormalRate = 80f;

    /// <summary>
    /// After what BPM the heartrate becomes critical
    /// </summary>
    [DataField]
    public float CriticalRate = 290f;

    /// <summary>
    /// After CriticalRate is reached, every second the heart has an X% chance of stopping
    /// </summary>
    [DataField]
    public float CriticalStopChance = 0.1f;

    /// <summary>
    /// Scale of the rate stabilization formula
    /// cbrt((cur - norm)(cur - minfib)(cur - maxfib))
    /// </summary>
    [DataField]
    public float RateUpdateModifier = 0.02f;

    /// <summary>
    /// Heart rate bounds outside which fibrillation occurs and the rate drifts away from normal.
    /// </summary>
    [DataField]
    public Vector2 FibrillationCaps = new(40f, 210f);

    [ViewVariables, AutoNetworkedField]
    public float CurrentRate;

    [DataField]
    public ProtoId<AlertPrototype>? FibrillationAlert = "Fibrillations";

    [DataField]
    public ProtoId<AlertPrototype>? HeartStopAlert = "HeartStop";
}

public enum HeartState
{
    Stable,
    Fibrillating,
    Stopped,
}
