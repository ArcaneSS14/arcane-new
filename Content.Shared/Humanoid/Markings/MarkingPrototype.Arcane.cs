using Robust.Shared.Prototypes; // Arcane

namespace Content.Shared.Humanoid.Markings; // Arcane

// Arcane-Start
public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Ear layers that follow the character's ears-above-hair preference.
    /// </summary>
    [DataField]
    public bool UnderHair { get; private set; }

    /// <summary>
    /// Animated counterpart. Existing paired "Animated" IDs remain supported.
    /// </summary>
    [DataField]
    public ProtoId<MarkingPrototype>? Wagging;
}
// Arcane-End
