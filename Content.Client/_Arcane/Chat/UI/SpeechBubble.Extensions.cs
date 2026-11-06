using Content.Shared.Chat;

namespace Content.Client.Chat.UI;

public abstract partial class SpeechBubble
{
    /// <summary>
    /// Optional presentation factory, evaluated before the standard speech bubbles.
    /// </summary>
    public static Func<SpeechType, ChatMessage, EntityUid, SpeechBubble?>? CustomBubbleFactory { get; set; }
}
