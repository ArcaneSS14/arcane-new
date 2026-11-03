using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Arcane.Common.Preferences; // Arcane

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _allowFlavorText;

    private FlavorText.ExtendedFlavorEditor? _flavorText; // Arcane-Edit: extended flavor text
    private TextEdit? _flavorTextEdit;

    /// <summary>
    /// Refreshes the flavor text editor status.
    /// </summary>
    public void RefreshFlavorText()
    {
        if (_allowFlavorText)
        {
            if (_flavorText != null)
                return;

            _flavorText = new FlavorText.ExtendedFlavorEditor(); // Arcane-Edit
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));
            _flavorTextEdit = _flavorText.CFlavorTextInput;

            _flavorText.OnFlavorTextChanged += OnFlavorTextChange;
            // Arcane-Start
            _flavorText.OnExtendedChanged += OnExtendedFlavorChange;
            UpdateFlavorTextEdit();
            UpdateFlavorPreview();
            // Arcane-End
        }
        else
        {
            if (_flavorText == null)
                return;

            TabContainer.RemoveChild(_flavorText);
            _flavorText.OnFlavorTextChanged -= OnFlavorTextChange;
            _flavorText.OnExtendedChanged -= OnExtendedFlavorChange; // Arcane
            _flavorText.Dispose();
            _flavorTextEdit?.Dispose();
            _flavorTextEdit = null;
            _flavorText = null;
        }
    }

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        SetDirty();
    }

    private void UpdateFlavorTextEdit()
    {
        // Arcane-Edit-Start: the editor fills all flavor sections and its preview
        if (_flavorText != null && Profile != null)
            _flavorText.SetFlavor(Profile.FlavorText, Profile.ExtendedFlavor);
        else if (_flavorTextEdit != null)
            _flavorTextEdit.TextRope = new Rope.Leaf(Profile?.FlavorText ?? "");
        // Arcane-Edit-End
    }

    // Arcane-Start: extended flavor text
    private void OnExtendedFlavorChange(ExtendedFlavorText flavor)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithExtendedFlavor(flavor);
        SetDirty();
    }

    private void UpdateFlavorPreview()
    {
        if (_flavorText == null || Profile == null)
            return;

        _flavorText.Preview.SetCharacter(Profile.Name, Profile.Species, Profile.Sex, Profile.Gender);
        _flavorText.Preview.SetEntity(SpriteView.PreviewDummy);
    }
    // Arcane-End
}
