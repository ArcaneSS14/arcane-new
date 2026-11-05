// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.GameTicking;

/// <summary>
/// In-game info similar to the lobby info but for separated game HUD.
/// </summary>
[Serializable, NetSerializable]
// Arcane-Edit-Start: send facts and localization keys instead of server-localized text.
public sealed class TickerInGameInfoEvent(
    int roundId,
    int playerCount,
    string? mapName,
    LocId? gameModeTitle,
    LocId? gameModeDescription) : EntityEventArgs
{
    public readonly int RoundId = roundId;
    public readonly int PlayerCount = playerCount;
    public readonly string? MapName = mapName;
    public readonly LocId? GameModeTitle = gameModeTitle;
    public readonly LocId? GameModeDescription = gameModeDescription;
}
// Arcane-Edit-End
