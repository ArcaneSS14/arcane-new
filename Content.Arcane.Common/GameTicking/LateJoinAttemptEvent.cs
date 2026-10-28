// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;

namespace Content.Arcane.Common.GameTicking;

/// <summary>
/// Raised broadcast before a player late-joins with a character. Cancelling keeps them in the lobby.
/// </summary>
[ByRefEvent]
public record struct LateJoinAttemptEvent(ICommonSession Player, string CharacterName, bool Cancelled = false);
