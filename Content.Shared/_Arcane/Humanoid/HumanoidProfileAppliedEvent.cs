// Arcane-Start
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
/// Raised on a mob after a character profile was applied to it, for Arcane data stored in the profile
/// (for example the TTS voice).
/// </summary>
[ByRefEvent]
public readonly record struct HumanoidProfileAppliedEvent(HumanoidCharacterProfile Profile);
// Arcane-End
