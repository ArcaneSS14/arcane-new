using Content.Arcane.Client.Chat.UI;
using Content.Arcane.Client.Options.UI.Tabs;
using Content.Arcane.Client.UserInterface.RichText;
using Content.Arcane.Common.CCVar;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Client.Options.UI;
using Content.Shared.Chat;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.Options;

/// <summary>Attaches Arcane presentation and options through the base client's extension points.</summary>
public sealed partial class CustomizationSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private ExamineSystem _examine = default!;

    public override void Initialize()
    {
        base.Initialize();
        SpeechBubble.CustomBubbleFactory = CreateBubble;
        OptionsMenu.ConfigureExtensions += ConfigureOptions;
        _examine.SetAdditionalMarkupTags(typeof(RadioChannelColorTag));
    }

    public override void Shutdown()
    {
        if (SpeechBubble.CustomBubbleFactory == CreateBubble)
            SpeechBubble.CustomBubbleFactory = null;
        OptionsMenu.ConfigureExtensions -= ConfigureOptions;
        _examine.SetAdditionalMarkupTags();
        base.Shutdown();
    }

    private SpeechBubble? CreateBubble(SpeechBubble.SpeechType type, ChatMessage message, EntityUid sender)
    {
        return _configuration.GetCVar(ArcaneCVars.ChatRunechat)
            ? new RunechatSpeechBubble(message, sender, type)
            : null;
    }

    private void ConfigureOptions(OptionsMenu menu)
    {
        var tab = new CustomizationTab();
        menu.Tabs.AddChild(tab);
        menu.Tabs.SetTabTitle(menu.Tabs.ChildCount - 1, Loc.GetString("ui-options-tab-customization"));
        menu.ReloadExtensions += tab.Control.ReloadValues;
    }
}
