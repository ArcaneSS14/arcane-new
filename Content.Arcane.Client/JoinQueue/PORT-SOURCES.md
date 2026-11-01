# Connection queue and waiting games

Adapted from [Goob #6338](https://github.com/Goob-Station/Goob-Station/pull/6338)
(Aidenkrz) and the completed old Arcane implementation, including
[Arcane #102](https://github.com/ArcaneSS14/arcane/pull/102) (MataVsn) and
[Arcane #200](https://github.com/ArcaneSS14/arcane/pull/200) (Vecortys).
Code remains under AGPL-3.0-or-later. Integration and Trauma adaptations: UmbiMax.

The existing `queue.enabled` server setting is off by default. When enabled,
waiting connections do not consume admitted-player slots or participate in round
readiness. FIFO order is assigned before asynchronous data loading. A waiting
player keeps their original place for two minutes after disconnecting, while an
offline reservation does not block an available game slot. Existing Trauma
administrator, temporary-connect and returning-player exemptions remain authoritative.
Patron roles and donor priority are not imported.

The waiting screen is separate from the existing lobby. Its Gyruss,
GoGoShitcurity and Space Invaders games and temporary leaderboards do not grant
entry or change queue order. Scores are bounded and accepted only from the
current waiting connection. Network messages belong to the client application's
lifetime; world systems are recreated on reconnect.

Audio under `Resources/Audio/_Arcane/MiniGames` is CC0-1.0; its original
`attributions.yml` retains the four Freesound links. The spacepod sprite is
CC-BY-SA-3.0, credited to Paradise Station / Space Station 13 in its RSI metadata.
Existing Trauma fonts and creature sprites are reused.
