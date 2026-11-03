// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.GameTicking;

namespace Content.Trauma.Client.UserActions;

public sealed partial class StatusControlSystem : EntitySystem
{
    public TickerInGameInfoEvent? Info; // Arcane-Edit: string Info = string.Empty > unresolved in-game info

    public event Action? OnInfoUpdated;

    [SubscribeNetworkEvent]
    private void OnGetInfo(TickerInGameInfoEvent args)
    {
        Info = args; // Arcane-Edit: args.Text > args
        OnInfoUpdated?.Invoke();
    }

    // Arcane-Start
    public string GetInfoText()
    {
        if (Info is not { GameModeTitle: { } gameModeTitle, GameModeDescription: { } gameModeDescription } info)
            return string.Empty;

        return Loc.GetString(
            "game-ticker-get-ingame-info-text",
            ("roundId", info.RoundId),
            ("playerCount", info.PlayerCount),
            ("mapName", info.MapName ?? Loc.GetString("game-ticker-no-map-selected")),
            ("gmTitle", Loc.GetString(gameModeTitle)),
            ("desc", Loc.GetString(gameModeDescription)));
    }
    // Arcane-End
}
