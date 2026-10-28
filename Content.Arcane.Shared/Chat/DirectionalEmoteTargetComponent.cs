namespace Content.Arcane.Shared.Chat;

/// <summary>
/// Can send and receive directional emotes.
/// </summary>
[RegisterComponent]
public sealed partial class DirectionalEmoteTargetComponent : Component
{
    [ViewVariables]
    public TimeSpan LastSend = TimeSpan.Zero;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed class SendDirectionalEmoteEvent(NetEntity target, string text) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly string Text = text;
}
