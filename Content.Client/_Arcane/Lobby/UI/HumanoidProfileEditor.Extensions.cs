// Arcane-Start
using Content.Shared.Preferences;
using Content.Shared.TTS;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Lets Arcane client code (Content.Arcane.Client) add its own tabs to the character editor.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Raised once per editor when it is created.
    /// </summary>
    public static event Action<HumanoidProfileEditor>? ConfigureExtensions;

    /// <summary>
    /// Raised when the edited profile changes, including from the editor's own controls.
    /// </summary>
    public event Action<HumanoidCharacterProfile?>? OnProfileEdited;

    /// <summary>
    /// Inserts a tab at <paramref name="position"/> (or at the end).
    /// </summary>
    public void AddExtensionTab(Control tab, string title, int position = -1)
    {
        TabContainer.AddChild(tab);
        if (position >= 0 && position < TabContainer.ChildCount - 1)
            tab.SetPositionInParent(position);

        TabContainer.SetTabTitle(tab.GetPositionInParent(), title);
    }

    public void SetTtsVoice(ProtoId<TTSVoicePrototype> voice)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithTtsVoice(voice);
        SetDirty();
        OnProfileEdited?.Invoke(Profile);
    }

    private void InitializeExtensions()
    {
        OnSetProfile += profile => OnProfileEdited?.Invoke(profile);
        ConfigureExtensions?.Invoke(this);
    }
}
// Arcane-End
