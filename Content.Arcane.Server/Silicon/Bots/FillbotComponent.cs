using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Server.Silicon.Bots;

/// <summary>
/// A service bot which carries loose items to its linked receiver.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class FillbotComponent : Component
{
    [DataField]
    public TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public EntityUid? FailedItem;

    [ViewVariables]
    public EntityUid? FailedReceiver;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RetryAfter;
}
