// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Barks;
using Content.Trauma.Common.Knowledge;
using Content.Shared.Preferences;
using Robust.Shared.Timing;
using Content.Shared.Humanoid.Prototypes; // Arcane

namespace Content.Client.Lobby.UI;

/// <summary>
/// Trauma - barks specific stuff and slider optimisation
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    [Dependency] private IGameTiming _timing = default!;
    private uint _lastColorUpdate;

    /// <summary>
    /// For other systems to do stuff
    /// </summary>
    public event Action<HumanoidCharacterProfile?>? OnSetProfile;

    private void InitializeTrauma()
    {
        IoCManager.InjectDependencies(this); // did you know IoC exists? now you do

        if (_cfgManager.GetCVar(GoobCVars.BarksEnabled))
        {
            BarksContainer.Visible = true;
            InitializeBarkVoice();
        }

        OnSetProfile += _ => UpdateBarkVoice(); // TODO: move bark shitcode into module

        // Arcane-Start: Wire character growth controls
        HeightSlider.OnValueChanged += _ => UpdateDimensions(true);
        WidthSlider.OnValueChanged += _ => UpdateDimensions(false);
        HeightReset.OnPressed += _ => ResetDimension(true);
        WidthReset.OnPressed += _ => ResetDimension(false);
        // Arcane-End
    }

    // Arcane-Start: Keep sliders, profile values, labels, and preview in sync
    private void UpdateGrowthControls()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        HeightSlider.MinValue = 0;
        HeightSlider.MaxValue = 2;
        HeightSlider.SetValueWithoutEvent(Profile.Height);
        HeightSlider.MinValue = species.MinHeight;
        HeightSlider.MaxValue = species.MaxHeight;
        WidthSlider.MinValue = 0;
        WidthSlider.MaxValue = 2;
        WidthSlider.SetValueWithoutEvent(Profile.Width);
        WidthSlider.MinValue = species.MinWidth;
        WidthSlider.MaxValue = species.MaxWidth;
        UpdateGrowthLabels(species);
    }

    private void ResetDimension(bool height)
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        if (height)
            Profile = Profile.WithHeight(species.DefaultHeight);
        else
            Profile = Profile.WithWidth(species.DefaultWidth);
        UpdateGrowthControls();
        SpriteView.ReloadProfilePreview(Profile);
        IsDirty = true;
    }

    private void UpdateDimensions(bool heightChanged)
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var sizeRatio = Math.Max(1f, species.SizeRatio);
        var height = Math.Clamp(HeightSlider.Value, species.MinHeight, species.MaxHeight);
        var width = Math.Clamp(WidthSlider.Value, species.MinWidth, species.MaxWidth);
        var ratio = height / width;
        if (heightChanged && (ratio < 1f / sizeRatio || ratio > sizeRatio))
            width = Math.Clamp(height / Math.Clamp(ratio, 1f / sizeRatio, sizeRatio), species.MinWidth, species.MaxWidth);
        else if (!heightChanged && (ratio < 1f / sizeRatio || ratio > sizeRatio))
            height = Math.Clamp(width * Math.Clamp(ratio, 1f / sizeRatio, sizeRatio), species.MinHeight, species.MaxHeight);

        Profile = Profile.WithHeight(height).WithWidth(width);
        HeightSlider.SetValueWithoutEvent(height);
        WidthSlider.SetValueWithoutEvent(width);
        UpdateGrowthLabels(species);
        SpriteView.ReloadProfilePreview(Profile);
        IsDirty = true;
    }

    private void UpdateGrowthLabels(SpeciesPrototype species)
    {
        if (Profile == null)
            return;

        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int)MathF.Round(species.AverageHeight * Profile.Height)));
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("width", (int)MathF.Round(species.AverageWidth * Profile.Width)));
        WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", (int)MathF.Round(species.AverageWeight * (Profile.Height + Profile.Width) / 2f)));
    }
    // Arcane-End

    private void SetBarkVoice(BarkPrototype newVoice)
    {
        Profile = Profile?.WithBarkVoice(newVoice);
        IsDirty = true;
    }

    private void SetKnowledge(KnowledgeProfile profile)
    {
        Profile = Profile?.WithKnowledge(profile);
        IsDirty = true;
    }
}
