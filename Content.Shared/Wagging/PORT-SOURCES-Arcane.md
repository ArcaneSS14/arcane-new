# Marking-dependent wagging actions

Adapted from [Goob #7459](https://github.com/Goob-Station/Goob-Station/pull/7459)
and the resulting Goob source at `45e8d6b1cb1d3464762c8a50ce2f7f5ddf6e59c1`,
under AGPL-3.0-or-later. The large marking/texture catalog is excluded.

The existing Trauma tail organ continues to grant/remove the Wagging component.
The current visual-body API notifies consumers of direct organ marking changes.
The server updates action availability after those edits finish; the existing
shared toggle continues to apply the visual change. Explicit animated counterpart
IDs and existing compatible `Animated` pairs use the current MarkingManager cache.
Custom component suffixes retain their existing meaning. Removed actions are
deleted from their body provider instead of accumulating across tail changes.
