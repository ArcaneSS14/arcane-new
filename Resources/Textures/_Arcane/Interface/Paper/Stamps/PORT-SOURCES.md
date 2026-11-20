# Graphical paper stamps

Source: [Goob-Station/Goob-Station](https://github.com/Goob-Station/Goob-Station) at `cc1d2c911f5cb6c90b36e0899ce918010bd41262`, `Resources/Textures/_Goobstation/Interface/Paper/Stamps/`.

The graphical stamp feature was introduced by [Goob #6175](https://github.com/Goob-Station/Goob-Station/pull/6175), commit `181d900ca35d6c7cff5fc43728b64a622d0ce530`, by fishbait. [Goob #6400](https://github.com/Goob-Station/Goob-Station/pull/6400), commit `6e1a401d729c942ab506f9dbf7bc81e69e719333`, is the related paper forms feature; it does not change these images.

All 15 PNGs here are unchanged copies of the same-named files in [tgstation/tgstation, icons/stamp_icons](https://github.com/tgstation/tgstation/tree/9a9b428b61f388a0f6d7a20bbba05147e61023d0/icons/stamp_icons). Their Git blob hashes match both that original revision and the pinned Goob revision.

The original [README license section](https://github.com/tgstation/tgstation/blob/9a9b428b61f388a0f6d7a20bbba05147e61023d0/README.md#license) places assets under Creative Commons Attribution-ShareAlike 3.0 unless otherwise indicated. The source `attributions.yml` declares the same license and credits “Stamp icons taken from tgstation”; that notice is preserved here. No individual artist name is inferred from the port author.

Changes for Arcane: file paths only. `attributions.yml` retains the original TG entry, excluding the unimported `large_stamp-void.png`; the separate, unimported hamster entry is omitted. No image, color, scale or compression changes were made. No extra font is included. The prototype values are adapted from the AGPL-3.0-or-later Goob entity prototypes into an Arcane partial, without copying their unrelated definitions.

`stampLargeIcon` values are complete paths relative to `Resources/Textures`, including `.png`. Existing stamp names, ink colors, world sprite states, item artwork, inventories and distribution remain owned by their existing prototypes. Color is already present in these PNGs and should not be multiplied by the stamp ink color when displaying them.
