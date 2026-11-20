namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    /// <summary>
    /// Characters of this species can set a custom species name in the character editor.
    /// </summary>
    [DataField]
    public bool CustomName { get; private set; }
}
