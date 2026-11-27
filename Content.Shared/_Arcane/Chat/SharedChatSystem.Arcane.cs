// Arcane-Start
namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    /// <summary>
    /// Sends an emote to chat without matching it against emote chat triggers, for callers that play their own
    /// sound for the message. Does nothing on the client.
    /// </summary>
    public void SendEmoteWithoutTriggers(EntityUid source, string action, bool hideChat = false, bool hideLog = false)
    {
        var range = hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal;
        SendEntityEmote(source, action, range, null, _language.GetLanguage(source), hideLog: hideLog, checkEmote: false);
    }
}
// Arcane-End
