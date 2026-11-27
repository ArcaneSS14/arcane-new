namespace Content.Arcane.Shared.Invisibility;

/// <summary>
/// An organ that keeps its body invisible: inserting it adds <see cref="ArcaneInvisibilityComponent"/> to the body,
/// cutting it out removes it.
/// </summary>
[RegisterComponent]
public sealed partial class ArcaneInvisibilityOrganComponent : Component;
