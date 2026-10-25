// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Medical;

// Arcane-Edit-Start: Full CPR and chest compressions share one interaction.
/// <summary>
/// Allows a mob to perform CPR or chest compressions with a verb.
/// Only full CPR requires the performer to provide rescue breaths.
/// </summary>
// Arcane-Edit-End
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
public sealed partial class CPRTrainingComponent : Component
{
    /// <summary>
    /// How long the doafter lasts.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3); // Arcane-Edit: 4 > 3

    // Arcane-Start
    /// <summary>
    /// The delay between uninterrupted compression cycles after the first cycle.
    /// </summary>
    [DataField]
    public TimeSpan RepeatDuration = TimeSpan.FromSeconds(0.8);
    // Arcane-End

    /// <summary>
    /// Chance to attempt reviving after a compression, if conditions are met.
    /// </summary>
    [DataField]
    public float ReviveChance = 0.3f;

    /// <summary>
    /// Chance to make the patient inhale after a compression, if their lungs are healthy.
    /// </summary>
    [DataField]
    public float InhaleChance = 0.6f; // better than nothing but proper training would help

    /// <summary>
    /// Sound played after each compression.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Effects/CPR.ogg");
}
