// Arcane-Start
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype : ISerializationHooks
{
    /// <summary>
    /// Characters of this species can set a custom species name in the character editor.
    /// </summary>
    [DataField]
    public bool CustomName { get; private set; }

    void ISerializationHooks.AfterDeserialization()
    {
        // DefaultSoundsBySex is indexed by (int) Sex. Futanari sounds like the female voice unless a species lists one.
        if (DefaultSoundsBySex.Length == (int) Sex.Futanari)
            DefaultSoundsBySex = [.. DefaultSoundsBySex, DefaultSoundsBySex[(int) Sex.Female]];
    }
}
// Arcane-End
