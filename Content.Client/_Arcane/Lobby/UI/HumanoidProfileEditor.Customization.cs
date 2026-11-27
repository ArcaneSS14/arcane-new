// Arcane-Start
using Content.Arcane.Common.Preferences;
using Content.Client.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Custom species name and the hair gradient in the character editor.
/// The controls are built here so the editor's XAML stays as it is.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private BoxContainer _customSpeciesNameRow = default!;
    private LineEdit _customSpeciesNameEdit = default!;
    private CheckBox _hairGradientEnabled = default!;
    private BoxContainer _hairGradientControls = default!;
    private OptionButton _hairGradientStyle = default!;
    private Label _hairGradientRootsLabel = default!;
    private Label _hairGradientTipsLabel = default!;
    private Label _hairGradientOffsetLabel = default!;
    private ColorSelectorSliders _hairGradientRoots = default!;
    private ColorSelectorSliders _hairGradientTips = default!;
    private Slider _hairGradientOffset = default!;
    private HairGradientHarmony? _hairGradientHarmony;
    private int _hairGradientVariant;
    private bool _updatingCustomization;

    private void InitializeCustomization()
    {
        // Custom species name, right under the species row.
        _customSpeciesNameEdit = new LineEdit { MinSize = new(270, 0), HorizontalAlignment = HAlignment.Right };
        _customSpeciesNameRow = new BoxContainer { HorizontalExpand = true, Visible = false };
        _customSpeciesNameRow.AddChild(new Label { Text = Loc.GetString("humanoid-profile-editor-custom-species-name-label") });
        _customSpeciesNameRow.AddChild(new Control { HorizontalExpand = true });
        _customSpeciesNameRow.AddChild(_customSpeciesNameEdit);
        if (SpeciesButton.Parent is { Parent: { } speciesList } speciesRow)
        {
            speciesList.AddChild(_customSpeciesNameRow);
            _customSpeciesNameRow.SetPositionInParent(speciesRow.GetPositionInParent() + 1);
        }

        _customSpeciesNameEdit.OnTextChanged += args => EditCustomization(c => c.CustomSpeciesName = args.Text);

        // The hair gradient, above the marking picker.
        var panel = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new(0, 0, 0, 6) };

        _hairGradientEnabled = new CheckBox { Text = Loc.GetString("humanoid-profile-editor-hair-gradient") };
        _hairGradientEnabled.OnToggled += args => OnHairGradientToggled(args.Pressed);
        panel.AddChild(_hairGradientEnabled);

        _hairGradientControls = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Visible = false };
        var styleRow = new BoxContainer();
        styleRow.AddChild(new Label { Text = Loc.GetString("humanoid-profile-editor-hair-gradient-style-label") });
        _hairGradientStyle = new OptionButton { HorizontalExpand = true };
        foreach (var style in Enum.GetValues<HairGradientStyle>())
            _hairGradientStyle.AddItem(Loc.GetString($"humanoid-profile-editor-hair-gradient-style-{style.ToString().ToLowerInvariant()}"), (int) style);
        _hairGradientStyle.OnItemSelected += args =>
        {
            _hairGradientStyle.SelectId(args.Id);
            EditCustomization(c => c.HairGradientStyle = (HairGradientStyle) args.Id);
        };
        styleRow.AddChild(_hairGradientStyle);
        _hairGradientControls.AddChild(styleRow);

        _hairGradientRootsLabel = new Label();
        _hairGradientRoots = new ColorSelectorSliders { SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv };
        _hairGradientRoots.OnColorChanged += _ =>
        {
            _hairGradientHarmony = null;
            OnHairGradientColorsChanged();
        };
        _hairGradientTipsLabel = new Label();
        _hairGradientTips = new ColorSelectorSliders { SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv };
        _hairGradientTips.OnColorChanged += _ => OnHairGradientColorsChanged();
        _hairGradientControls.AddChild(_hairGradientRootsLabel);
        _hairGradientControls.AddChild(_hairGradientRoots);
        _hairGradientControls.AddChild(_hairGradientTipsLabel);
        _hairGradientControls.AddChild(_hairGradientTips);

        var suggestions = new BoxContainer();
        suggestions.AddChild(SuggestionButton("natural", () => SuggestHairGradientTips(HairGradientHarmony.Lighten)));
        suggestions.AddChild(SuggestionButton("analogous", () => SuggestHairGradientTips(HairGradientHarmony.Analogous)));
        suggestions.AddChild(SuggestionButton("contrast", () => SuggestHairGradientTips(HairGradientHarmony.Contrast)));
        suggestions.AddChild(SuggestionButton("swap", () =>
        {
            (_hairGradientRoots.Color, _hairGradientTips.Color) = (_hairGradientTips.Color, _hairGradientRoots.Color);
            _hairGradientHarmony = null;
            OnHairGradientColorsChanged();
        }));
        _hairGradientControls.AddChild(suggestions);

        _hairGradientOffsetLabel = new Label();
        _hairGradientOffset = new Slider { MinValue = 0, MaxValue = 100, HorizontalExpand = true };
        _hairGradientOffset.OnValueChanged += _ =>
        {
            UpdateHairGradientLabels();
            EditCustomization(c => c.HairGradientOffset = _hairGradientOffset.Value / 100f);
        };
        _hairGradientControls.AddChild(_hairGradientOffsetLabel);
        _hairGradientControls.AddChild(_hairGradientOffset);
        panel.AddChild(_hairGradientControls);

        MarkingsTab.AddChild(panel);
        panel.SetPositionInParent(0);

        OnSetProfile += _ => UpdateCustomization();
        InitializeErp();
    }

    private Button SuggestionButton(string name, Action onPressed)
    {
        var button = new Button
        {
            Text = Loc.GetString($"humanoid-profile-editor-hair-gradient-btn-{name}"),
            ToolTip = Loc.GetString($"humanoid-profile-editor-hair-gradient-btn-{name}-tooltip"),
            HorizontalExpand = true,
        };
        button.OnPressed += _ => onPressed();
        return button;
    }

    /// <summary>
    /// Puts the profile's customization into the controls; call after the profile or its species changes.
    /// </summary>
    private void UpdateCustomization()
    {
        if (Profile == null)
            return;

        _updatingCustomization = true;
        var customization = Profile.Customization;
        var allowsName = _prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species) && species.CustomName;
        _customSpeciesNameRow.Visible = allowsName;
        if (_customSpeciesNameEdit.Text != customization.CustomSpeciesName)
            _customSpeciesNameEdit.Text = customization.CustomSpeciesName;

        _hairGradientEnabled.Pressed = customization.HairGradientEnabled;
        _hairGradientControls.Visible = customization.HairGradientEnabled;
        _hairGradientStyle.SelectId((int) customization.HairGradientStyle);
        _hairGradientRoots.Color = customization.HairGradientColors.Count > 0 ? customization.HairGradientColors[0] : Color.Black;
        _hairGradientTips.Color = customization.HairGradientColors.Count > 1 ? customization.HairGradientColors[1] : _hairGradientRoots.Color;
        _hairGradientOffset.Value = customization.HairGradientOffset * 100f;
        UpdateHairGradientLabels();
        _updatingCustomization = false;
    }

    private void EditCustomization(Action<CharacterCustomization> edit)
    {
        if (Profile == null || _updatingCustomization)
            return;

        var customization = Profile.Customization.Clone();
        edit(customization);
        Profile = Profile.WithCustomization(customization);
        UpdateHairGradientLabels();
        ReloadProfilePreview();
    }

    private void OnHairGradientToggled(bool enabled)
    {
        _hairGradientControls.Visible = enabled;
        // Identical colours mean the gradient was never set up: start from black roots with lighter tips.
        if (enabled && _hairGradientRoots.Color == _hairGradientTips.Color)
        {
            _updatingCustomization = true;
            _hairGradientTips.Color = HairGradientPalette.Suggest(_hairGradientRoots.Color, HairGradientHarmony.Lighten, 0);
            _updatingCustomization = false;
        }

        EditCustomization(c =>
        {
            c.HairGradientEnabled = enabled;
            c.HairGradientColors = new List<Color> { _hairGradientRoots.Color, _hairGradientTips.Color };
        });
    }

    private void OnHairGradientColorsChanged()
    {
        if (_updatingCustomization)
            return;

        // Dragging a colour slider fires every frame; one profile update per frame keeps the preview responsive.
        var now = _timing.CurFrame;
        if (_lastColorUpdate == now)
            return;

        _lastColorUpdate = now;
        EditCustomization(c => c.HairGradientColors = new List<Color> { _hairGradientRoots.Color, _hairGradientTips.Color });
    }

    private void SuggestHairGradientTips(HairGradientHarmony harmony)
    {
        // Pressing the same harmony again steps through its variants.
        _hairGradientVariant = _hairGradientHarmony == harmony ? _hairGradientVariant + 1 : 0;
        _hairGradientHarmony = harmony;
        _updatingCustomization = true;
        _hairGradientTips.Color = HairGradientPalette.Suggest(_hairGradientRoots.Color, harmony, _hairGradientVariant);
        _updatingCustomization = false;
        EditCustomization(c => c.HairGradientColors = new List<Color> { _hairGradientRoots.Color, _hairGradientTips.Color });
    }

    private void UpdateHairGradientLabels()
    {
        var style = ((HairGradientStyle) _hairGradientStyle.SelectedId).ToString().ToLowerInvariant();
        _hairGradientRootsLabel.Text = Loc.GetString($"humanoid-profile-editor-hair-gradient-roots-{style}");
        _hairGradientTipsLabel.Text = Loc.GetString($"humanoid-profile-editor-hair-gradient-tips-{style}");
        _hairGradientOffsetLabel.Text = Loc.GetString($"humanoid-profile-editor-hair-gradient-offset-{style}", ("value", (int) MathF.Round(_hairGradientOffset.Value)));
    }
}
// Arcane-End
