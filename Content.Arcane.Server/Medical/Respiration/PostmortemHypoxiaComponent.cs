using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Tracks untreated suffocation only while a dead breathing body is below its postmortem limit.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PostmortemHypoxiaComponent : Component
{
    /// <summary>
    /// Last observed suffocation damage, updated by body threshold notifications.
    /// </summary>
    [DataField]
    public FixedPoint2 Asphyxiation;

    /// <summary>
    /// Half the body's current critical threshold; the postmortem cap is twice this value.
    /// </summary>
    [DataField]
    public FixedPoint2 LowerThreshold;

    /// <summary>
    /// Earliest time for untreated suffocation to increase again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDamage;

    /// <summary>
    /// Time through which the current stasis pause has been accounted for.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastUpdate;
}
