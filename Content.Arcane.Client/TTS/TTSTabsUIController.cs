using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.Arcane.Shared.TTS;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Arcane.Client.TTS;

/// <summary>
/// Adds the TTS settings tab to the options menu and the voice tab to the character editor.
/// </summary>
[UsedImplicitly]
public sealed class TTSTabsUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();
        OptionsMenu.ConfigureExtensions -= ConfigureOptions;
        OptionsMenu.ConfigureExtensions += ConfigureOptions;
        HumanoidProfileEditor.ConfigureExtensions -= ConfigureEditor;
        HumanoidProfileEditor.ConfigureExtensions += ConfigureEditor;
    }

    private static void ConfigureOptions(OptionsMenu menu)
    {
        var tab = new TTSOptionsTab();
        menu.Tabs.AddChild(tab);
        menu.Tabs.SetTabTitle(menu.Tabs.ChildCount - 1, Loc.GetString("ui-options-tab-tts"));
        menu.ReloadExtensions += () =>
        {
            tab.Control.ReloadValues();
            tab.RefreshChannelIcons();
        };
    }

    // Static on purpose: the editor events are static, an instance handler would keep every old client alive.
    private static void ConfigureEditor(HumanoidProfileEditor editor)
    {
        var tab = new TTSTab();
        editor.AddExtensionTab(tab, Loc.GetString("humanoid-profile-editor-voice-tab"));

        tab.OnVoiceSelected += voiceId =>
        {
            editor.SetTtsVoice(voiceId);
            tab.SetSelectedVoice(voiceId);
        };
        tab.OnPreviewRequested += voiceId =>
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TTSSystem>().RequestGlobalTTS(VoiceRequestType.Preview, voiceId);

        editor.OnProfileEdited += profile =>
        {
            if (profile == null)
                return;

            tab.UpdateControls(profile, profile.Sex);
            tab.SetSelectedVoice(profile.TtsVoice);
        };
    }
}
