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

    /// <summary>
    /// Whether this entity may send emotes without its name, as narration. Given to admin ghosts.
    /// </summary>
    [DataField]
    public bool CanHideName;
}

[Serializable, NetSerializable]
public sealed class SendDirectionalEmoteEvent(NetEntity target, string text, bool hideName = false) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly string Text = text;

    /// <summary>
    /// Send the text without the sender's name; only honoured for <see cref="DirectionalEmoteTargetComponent.CanHideName"/>.
    /// </summary>
    public readonly bool HideName = hideName;
}
