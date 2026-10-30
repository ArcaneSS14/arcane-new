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
    /// <summary>
    /// The slider works in centimetres. The sprite scale grows 1.2 times slower than the announced height,
    /// so a 20% taller character only becomes ~16.7% bigger on screen.
    /// </summary>
    private const float GrowthDamping = 1.2f;

    private bool _updatingGrowth;

    private static float BaseScale(SpeciesPrototype species)
        => species.DefaultScale > 0f ? species.DefaultScale : 1f;

    private static float BaseHeight(SpeciesPrototype species)
        => MathF.Max(species.AverageHeight, 1f);

    /// <remarks>
    /// The species average height corresponds to its default scale, so every deviation from it is divided by
    /// <see cref="GrowthDamping"/> before it reaches the sprite.
    /// </remarks>
    private static float HeightToScale(SpeciesPrototype species, float height)
        => BaseScale(species) * (1f + (height / BaseHeight(species) - 1f) / GrowthDamping);

    private static float ScaleToHeight(SpeciesPrototype species, float scale)
        => BaseHeight(species) * (1f + GrowthDamping * (scale / BaseScale(species) - 1f));

    private static float MinGrowthHeight(SpeciesPrototype species)
        => ScaleToHeight(species, species.MinScale);

    private static float MaxGrowthHeight(SpeciesPrototype species)
        => ScaleToHeight(species, species.MaxScale);

    private void UpdateGrowthControls()
    {
        if (Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var height = Math.Clamp(ScaleToHeight(species, Profile.Scale), MinGrowthHeight(species), MaxGrowthHeight(species));

        _updatingGrowth = true;
        ScaleSlider.MaxValue = MaxGrowthHeight(species);
        ScaleSlider.MinValue = MinGrowthHeight(species);
        ScaleSlider.SetValueWithoutEvent(height);
        _updatingGrowth = false;
        UpdateGrowthLabels(species, height);
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
        if (_updatingGrowth || Profile == null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var height = MathF.Round(Math.Clamp(ScaleSlider.Value, MinGrowthHeight(species), MaxGrowthHeight(species)));
        var scale = Math.Clamp(HeightToScale(species, height), species.MinScale, species.MaxScale);
        height = ScaleToHeight(species, scale);

        Profile = Profile.WithScale(scale);
        _updatingGrowth = true;
        ScaleSlider.SetValueWithoutEvent(height);
        _updatingGrowth = false;
        UpdateGrowthLabels(species, height);
        SpriteView.ReloadProfilePreview(Profile);
        SetDirty();
    }

    private void UpdateGrowthLabels(SpeciesPrototype species, float height)
    {
        if (Profile == null)
            return;

        ScaleLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int) MathF.Round(height)));
        WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", (int) MathF.Round(species.AverageWeight * Profile.Scale)));
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
