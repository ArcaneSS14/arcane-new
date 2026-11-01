# PDA pen visibility

Behavior from Goob-Station/Goob-Station#6160 and final source
45e8d6b1cb1d3464762c8a50ce2f7f5ddf6e59c1 (AGPL-3.0-or-later).
Adapted to the existing ItemSlot.HasItem API instead of maintaining another
ContainedPen cache. Existing insert/remove/init updates and Appearance networking
remain the owners. Existing enum values, ID/access and slot rules are retained.

The insert_overlay texture already exists in Trauma's PDA RSI; its existing
CC-BY-SA-3.0 attribution is unchanged. No PDA resprite is imported. The client
updates the pen layer only when a prototype provides it.
