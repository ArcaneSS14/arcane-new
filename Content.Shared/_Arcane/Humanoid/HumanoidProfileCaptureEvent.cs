// Arcane-Start: let extension systems include their live state when capturing a profile
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

[ByRefEvent]
public record struct HumanoidProfileCaptureEvent(HumanoidCharacterProfile Profile);
// Arcane-End
