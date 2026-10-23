using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Server.Medical.Defibrillator;

/// <summary>
/// Tracks the patient's recovery interval across different defibrillators and operators.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DefibrillatorShockHistoryComponent : Component
{
    /// <summary>
    /// Earliest time at which another shock avoids the injury from repeated discharge.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSafeShock;
}
