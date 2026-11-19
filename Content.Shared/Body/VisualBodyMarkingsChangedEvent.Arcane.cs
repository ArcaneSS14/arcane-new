namespace Content.Shared.Body; // Arcane

// Arcane-Start
/// <summary>
/// Raised on the body after its visual organ's markings are changed through the shared API.
/// </summary>
[ByRefEvent]
public readonly record struct VisualBodyMarkingsChangedEvent(EntityUid Organ);
// Arcane-End
