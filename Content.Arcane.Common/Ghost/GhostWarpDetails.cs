// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Arcane.Common.Ghost;

[Serializable, NetSerializable]
public enum GhostWarpPlayerState : byte
{
    Alive,
    Dead,
    Left,
    Ghost,
}

[Serializable, NetSerializable]
public readonly record struct GhostWarpPlayerDetail(NetEntity Entity, string JobName, string? DepartmentId, GhostWarpPlayerState State);

[Serializable, NetSerializable]
public readonly record struct GhostWarpAntagonist(NetEntity Entity, string AntagonistName, string Description, int Weight);

/// <summary>
/// Sent to a ghost along with the vanilla warp list: who the players are and which entities are public antagonists,
/// so the warp menu can group them (Orion #107, #121).
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostWarpDetailsEvent(List<GhostWarpPlayerDetail> players, List<GhostWarpAntagonist> antagonists) : EntityEventArgs
{
    public readonly List<GhostWarpPlayerDetail> Players = players;
    public readonly List<GhostWarpAntagonist> Antagonists = antagonists;
}
