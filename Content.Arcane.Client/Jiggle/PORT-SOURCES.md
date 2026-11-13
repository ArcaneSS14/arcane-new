# Jiggle

Ported from [Goob-Station #7422](https://github.com/Goob-Station/Goob-Station/pull/7422), original commit `8594298dd281d09c9b1414a231fc1564f9b68156`, by Aiden <aiden@djkraz.com>. Verified final source: `cc1d2c911f5cb6c90b36e0899ce918010bd41262`.

The Client/Shared Jiggle implementation is AGPL-3.0-or-later under the source repository license. `Resources/Textures/_Arcane/Shaders/jiggle.swsl` is MIT under the source REUSE annotation for `Resources/Textures/**`. Attribution is retained in the adapted files.

Arcane adapts the transient emote effect to its existing typed effect-event path and owns the postshader lifecycle. Jiggle explicitly precedes Stealth, including when no outline shader is present. Only existing SlimePerson is configured. Trauma NPC/jelly rendering, emote availability, voice and species remain governed by their existing systems.
