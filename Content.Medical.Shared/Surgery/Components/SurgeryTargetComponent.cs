// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.Surgery;

/// <summary>
/// Allows a mob to be operated upon.
/// Automatically added for every <c>Body</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTargetComponent : Component
{
    /// <summary>
    /// Should be self-explanatory. Is used to process logic of dealing poison damage to a skeleton.
    /// </summary>
    [DataField]
    public bool SepsisImmune;

    // Arcane-Start
    /// <summary>
    /// Speed multiplier when operating on yourself. 1/3 means three times slower.
    /// </summary>
    [DataField]
    public float SelfSurgerySpeedModifier = 1f / 3f;
    // Arcane-End
}
