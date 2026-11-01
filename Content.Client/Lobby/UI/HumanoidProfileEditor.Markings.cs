using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        _markingsModel.OrganProfileData = _markingManager.GetProfileData(Profile.Species, Profile.Sex, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor);
        _markingsModel.OrganData = _markingManager.GetMarkingData(Profile.Species); // Trauma - moved from above because it was breaking the UI when switching species
        _markingsModel.Markings = Profile.Appearance.Markings;
        Markings.EarsAboveHair = Profile.Appearance.EarsAboveHair; // Arcane
    }

    private void OnMarkingChange()
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(_markingsModel.Markings));
        ReloadProfilePreview();
        SetDirty();
    }

    // <Arcane>
    private void OnEarsAboveHairChanged(bool value)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithEarsAboveHair(value));
        ReloadProfilePreview();
    }

    private void UpdateMarkingPreview()
    {
        if (Profile == null)
            return;

        SpriteView.ReloadProfilePreview(GetMarkingPreviewProfile());
    }

    private HumanoidCharacterProfile GetMarkingPreviewProfile()
    {
        var profile = Profile!;
        return profile.WithCharacterAppearance(profile.Appearance.WithMarkings(_markingsModel.GetPreviewMarkings()));
    }
    // </Arcane>
}
