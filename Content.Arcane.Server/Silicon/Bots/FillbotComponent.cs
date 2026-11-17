namespace Content.Arcane.Server.Silicon.Bots;

/// <summary>
/// A service bot which carries loose items to its linked receiver.
/// </summary>
[RegisterComponent]
public sealed partial class FillbotComponent : Component
{
    [DataField]
    public TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public readonly Dictionary<(EntityUid Item, EntityUid Sink), TimeSpan> BlockedItems = new();
}
