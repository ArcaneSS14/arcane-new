// Arcane-Start
using Robust.Shared.Audio;

namespace Content.Shared.Radio;

/// <summary>
/// Arcane - radio barks (Orion #452, #456).
/// </summary>
public sealed partial class RadioChannelPrototype
{
    /// <summary>
    /// Played to headset wearers when a message arrives on this channel. Falls back to a generic bark.
    /// </summary>
    [DataField]
    public SoundPathSpecifier? OnSendSound;
}
// Arcane-End
