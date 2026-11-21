using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModStatusReadoutComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public bool ShowFingerprint;
}
