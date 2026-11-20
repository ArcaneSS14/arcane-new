using Content.Arcane.Common.Preferences;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileComponent
{
    /// <summary>
    /// The profile's custom species name and hair gradient.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CharacterCustomization Customization = new();
}
