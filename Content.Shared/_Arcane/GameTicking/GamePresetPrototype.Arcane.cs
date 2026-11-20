namespace Content.Shared.GameTicking.Prototypes;

public sealed partial class GamePresetPrototype
{
    /// <summary>
    /// Weight multiplier per earlier round that used this preset, for the random preset pick.
    /// </summary>
    [DataField]
    public float RepeatWeightMultiplier = 0.5f;

    /// <summary>
    /// Extra weight multiplier when the previous round used this preset.
    /// </summary>
    [DataField]
    public float PreviousRoundWeightMultiplier = 0.5f;
}
