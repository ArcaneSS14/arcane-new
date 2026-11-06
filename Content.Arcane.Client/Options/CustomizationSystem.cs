using Content.Arcane.Client.Chat.UI;
using Content.Arcane.Client.UserInterface.RichText;
using Content.Arcane.Common.CCVar;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Shared.Chat;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.Options;

/// <summary>Registers Arcane speech bubbles and examine markup for the game session.</summary>
public sealed partial class CustomizationSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private ExamineSystem _examine = default!;

    public override void Initialize()
    {
        base.Initialize();
        SpeechBubble.CustomBubbleFactory = CreateBubble;
        _examine.SetAdditionalMarkupTags(typeof(RadioChannelColorTag));
    }

    public override void Shutdown()
    {
        if (SpeechBubble.CustomBubbleFactory == CreateBubble)
            SpeechBubble.CustomBubbleFactory = null;
        _examine.SetAdditionalMarkupTags();
        base.Shutdown();
    }

    private SpeechBubble? CreateBubble(SpeechBubble.SpeechType type, ChatMessage message, EntityUid sender)
    {
        return _configuration.GetCVar(ArcaneCVars.ChatRunechat)
            ? new RunechatSpeechBubble(message, sender, type)
            : null;
    }
}
