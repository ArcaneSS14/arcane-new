using Content.Shared.Tools.Components;

namespace Content.Shared.Tools.Systems;

/// <summary>
///     Raised when <see cref="WeldableComponent"/> has changed.
/// </summary>
[ByRefEvent]
public readonly record struct WeldableChangedEvent(bool IsWelded, EntityUid? User = null) // Arcane-Edit: bool IsWelded) > bool IsWelded, EntityUid? User = null)
{
    public readonly bool IsWelded = IsWelded;
    public readonly EntityUid? User = User; // Arcane
}
