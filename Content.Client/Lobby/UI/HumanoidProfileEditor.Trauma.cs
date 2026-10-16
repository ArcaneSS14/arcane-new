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

        // Arcane-Start: Wire character scale control
        ScaleSlider.OnValueChanged += _ => UpdateScale();
        ScaleReset.OnPressed += _ => ResetScale();
        // Arcane-End
    }

    // Arcane-Start: Keep slider, profile value, labels, and preview in sync
    private void UpdateGrowthControls()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        ScaleSlider.MinValue = 0;
        ScaleSlider.MaxValue = 2;
        ScaleSlider.SetValueWithoutEvent(Profile.Scale);
        ScaleSlider.MinValue = species.MinScale;
        ScaleSlider.MaxValue = species.MaxScale;
        ScaleSlider.SetValueWithoutEvent(Profile.Scale);
        UpdateGrowthLabels(species);
    }

    private void ResetScale()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        Profile = Profile.WithScale(species.DefaultScale);
        UpdateGrowthControls();
        SpriteView.ReloadProfilePreview(Profile);
        SetDirty();
    }

    private void UpdateScale()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var scale = Math.Clamp(ScaleSlider.Value, species.MinScale, species.MaxScale);
        Profile = Profile.WithScale(scale);
        ScaleSlider.SetValueWithoutEvent(scale);
        UpdateGrowthLabels(species);
        SpriteView.ReloadProfilePreview(Profile);
        SetDirty();
    }

    private void UpdateGrowthLabels(SpeciesPrototype species)
    {
        if (Profile == null)
            return;

        ScaleLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int)MathF.Round(species.AverageHeight * Profile.Scale)));
        WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", (int)MathF.Round(species.AverageWeight * Profile.Scale)));
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
