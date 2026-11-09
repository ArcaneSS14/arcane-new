using Content.Client.UserInterface.Controls;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.Chat.Highlights;

[GenerateTypedNameReferences]
public sealed partial class RoleHighlightEditorWindow : FancyWindow
{
    private readonly Func<string, RoleHighlightsSaveResult> _save;

    public RoleHighlightEditorWindow(
        JobPrototype job,
        string defaultHighlights,
        string? savedHighlights,
        Func<string, RoleHighlightsSaveResult> save)
    {
        RobustXamlLoader.Load(this);
        _save = save;

        Title = Loc.GetString("role-highlight-editor-title", ("job", job.LocalizedName));
        HelpButton.Visible = true;
        HelpButton.ToolTip = Loc.GetString("role-highlight-editor-help");
        ValidationLabel.ModulateSelfOverride = Color.Red;

        HighlightsInput.Placeholder = new Rope.Leaf(defaultHighlights);
        if (!string.IsNullOrEmpty(savedHighlights))
            HighlightsInput.TextRope = new Rope.Leaf(savedHighlights);

        HighlightsInput.OnTextChanged += _ => UpdateValidation();
        SaveButton.OnPressed += _ => Save();
        UpdateValidation();
    }

    private void UpdateValidation()
    {
        var tooLong = HighlightsInput.TextLength > RoleHighlightsSystem.MaxHighlightsLength;
        SaveButton.Disabled = tooLong;
        ValidationLabel.Visible = tooLong;
        ValidationLabel.Text = tooLong
            ? Loc.GetString(
                "role-highlight-editor-too-long",
                ("max", RoleHighlightsSystem.MaxHighlightsLength))
            : string.Empty;
    }

    private void Save()
    {
        var result = _save(Rope.Collapse(HighlightsInput.TextRope));
        switch (result)
        {
            case RoleHighlightsSaveResult.Success:
                Close();
                return;
            case RoleHighlightsSaveResult.TooLong:
                UpdateValidation();
                return;
            case RoleHighlightsSaveResult.Failed:
                ValidationLabel.Text = Loc.GetString("role-highlight-editor-save-failed");
                ValidationLabel.Visible = true;
                return;
        }
    }
}
