// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Power;

/// <summary>
/// Handheld device that transfers energy from its cell to batteries of other entities (Orion #218, #281, #294).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InducerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string PowerCellSlotId = "inducer_power_cell_slot";

    /// <summary>
    /// Joules per second taken from the inducer's cell.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TransferRate;

    [DataField, AutoNetworkedField]
    public List<int> AvailableTransferRates = new();

    /// <summary>
    /// Seconds per transfer step.
    /// </summary>
    [DataField]
    public float TransferDelay;

    /// <summary>
    /// Energy multiplier for loose entities (weapons, cells, clothing...).
    /// </summary>
    [DataField]
    public float TransferMultiplier;

    /// <summary>
    /// Energy multiplier for anchored machines, APCs and wall mounts.
    /// </summary>
    [DataField]
    public float StructureTransferMultiplier;

    [DataField]
    public float MaxDistance;
}

[Serializable, NetSerializable]
public sealed partial class InducerDoAfterEvent : SimpleDoAfterEvent;
