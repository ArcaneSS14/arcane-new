namespace Content.Arcane.Shared.Medical.Heart;

/// <summary>
/// Heart-rate effects of an existing defibrillator.
/// </summary>
[RegisterComponent]
public sealed partial class HeartDefibrillatorComponent : Component
{
    /// <summary>
    /// Pulse change on a beating heart, in BPM.
    /// </summary>
    [DataField]
    public float BpmZapHeal = 45f;

    /// <summary>
    /// Pulse after restarting a stopped heart, in BPM.
    /// </summary>
    [DataField]
    public float BpmZapHealFlatline = 210f;

    /// <summary>
    /// Whether the pulse change points towards the resting rate.
    /// </summary>
    [DataField]
    public bool AutoStabilisation = true;
}
