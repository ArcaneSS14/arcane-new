// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Medical;

/// <summary>
/// Component added to mobs which have someone performing CPR on them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
[AutoGenerateComponentState]
public sealed partial class ActiveCPRComponent : Component
{
    /// <summary>
    /// The sound currently being played.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Sound;

    // Arcane-Start: Identify the active procedure so old completion events cannot end its replacement.
    /// <summary>
    /// The single performer who owns this patient's active procedure.
    /// </summary>
    [DataField]
    public EntityUid? Performer;

    /// <summary>
    /// The existing DoAfter identifier, which stays the same through uninterrupted repeats.
    /// </summary>
    [DataField]
    public ushort? DoAfterIndex;

    /// <summary>
    /// Whether this cycle includes artificial breathing in addition to compressions.
    /// </summary>
    [DataField]
    public bool CanVentilate = true;
    // Arcane-End
}
