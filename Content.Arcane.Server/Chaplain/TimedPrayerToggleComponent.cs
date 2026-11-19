using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Server.Chaplain;

/// <summary>Temporary prayer activation performed by the existing item-toggle owner.</summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class TimedPrayerToggleComponent : Component
{
    /// <summary>How long a successful prayer activation lasts.</summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DeactivateAt;
}
