namespace Content.Shared.Humanoid; // Arcane

// Arcane-Start
public sealed partial class HumanoidCharacterAppearance
{
    /// <summary>
    /// Keeps ear markings above the hair instead of placing them underneath it.
    /// </summary>
    [DataField]
    public bool EarsAboveHair { get; set; }

    public HumanoidCharacterAppearance WithEarsAboveHair(bool value)
        => new(EyeColor, SkinColor, Markings, value);
}
// Arcane-End
