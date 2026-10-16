using Content.Shared.EntityEffects;

namespace Content.Arcane.Shared.Medical.Heart;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyHeartRate : EntityEffectBase<ModifyHeartRate>
{
    /// <summary>
    /// amount to add to the currentheartrate per metabolism tick
    /// negatives values slow the heard down, positive valued speed it up
    /// </summary>
    [DataField(required: true)]
    public float Amount;

    /// <summary>
    /// should the reagent ignore the stopped heart?
    /// this will restart the heart if it were at zero.
    /// </summary>
    [DataField]
    public bool HeartRestart;

    /// <summary>
    /// If true, the amount specified will try to go towards the startingheartrate
    /// if the amount specified is negative it will try to de-stabilise it instead
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
