---
name: prediction
description: Keep responsive shared interactions deterministic and free of duplicated side effects while server authority wins.
---

# Replay-safe shared behavior

Predict short player-driven interactions with locally known inputs and correctable results. The server owns protected outcomes, permissions, hidden data and persistence. Shared mutation is not universally forbidden: separate deterministic/reconciled state from irreversible privileged effects.

For each affected component mutation, spawn/delete, sound, popup, animation, cooldown or UI update decide whether it is deterministic, first-time-only, server-only or reconciled. Use current predicted feedback helpers rather than an uncoordinated client call plus server confirmation. Resolving text twice is different from showing it twice.

Do not use an unsynchronized random source during replay; use deterministic inputs or authoritative correction under [randomness](../randomness-and-determinism/SKILL.md). Text follows [localization-in-code](../localization-in-code/SKILL.md).

Select relevant repeated input, latency, rejection, observers, deletion or correction checks under [verification](../../rules/verification.md). Transport changes belong to [networking](../networking/SKILL.md); a serialization-only test may not exercise replay.
