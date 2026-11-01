using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Animated counterpart. Existing paired "Animated" IDs remain supported.
    /// </summary>
    [DataField]
    public ProtoId<MarkingPrototype>? Wagging;
}
