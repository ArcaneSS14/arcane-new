using Content.Shared.EntityEffects;

namespace Content.Arcane.Shared.Medical.Heart;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyHeartRate : EntityEffectBase<ModifyHeartRate>
{
    /// <summary>
    /// Change in beats per minute per metabolism tick. Negative values slow the heart; positive values speed it up.
    /// </summary>
    [DataField(required: true)]
    public float Amount;

    /// <summary>
    /// Whether this effect may restart a stopped heart.
    /// </summary>
    [DataField]
    public bool HeartRestart;

    /// <summary>
    /// Whether a positive amount moves the rate towards normal and a negative amount moves it away.
    /// </summary>
    [DataField]
    public bool AutoStabilisation;

    /// <summary>
    /// If not null, the reagent will not lower the heart rate below this value
    /// </summary>
    [DataField]
    public float? LowerCap;

    /// <summary>
    /// If not null, the reagent will not raise the heart rate above this value
    /// </summary>
    [DataField]
    public float? HigherCap;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var lines = new List<string>();

        var key = (AutoStabilisation, Amount >= 0) switch
        {
            (true, true) => "entity-effect-guidebook-modify-heart-rate-stabilise",
            (true, false) => "entity-effect-guidebook-modify-heart-rate-destabilise",
            (false, true) => "entity-effect-guidebook-modify-heart-rate-increase",
            _ => "entity-effect-guidebook-modify-heart-rate-decrease",
        };
        lines.Add(Loc.GetString(key, ("amount", Math.Abs(Amount))));

        if (LowerCap is { } lower)
            lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-lower-cap", ("cap", lower)));
        if (HigherCap is { } higher)
            lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-higher-cap", ("cap", higher)));

        if (HeartRestart)
            lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-restart"));

        return string.Join("\n", lines);
    }
}
