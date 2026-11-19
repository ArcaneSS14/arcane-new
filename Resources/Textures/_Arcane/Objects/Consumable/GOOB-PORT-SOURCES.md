# Goob consumable assets

Source repository: [Goob-Station/Goob-Station](https://github.com/Goob-Station/Goob-Station), revision `cc1d2c911f5cb6c90b36e0899ce918010bd41262`. The imported directories retain their relative paths from `Resources/Textures/_Goobstation/Objects/Consumable/` under the Arcane resource owner.

All imported PNG bytes are unchanged. Each RSI's `meta.json` remains the authority for its attribution and CC-BY-SA-3.0 license. Formatting, notices, animation timings, dimensions and directional contracts are preserved; the sole metadata change removes the duplicated `bowl` state from `Food/chilly_pepper_food.rsi`. Its consumer reuses the identical existing `Objects/Consumable/Food/bowl.rsi`, state `bowl`, with that asset's existing attribution.

| RSI | Source family | Preserved credit |
|---|---|---|
| Drinks/shaker_bluespace.rsi | [#6596](https://github.com/Goob-Station/Goob-Station/pull/6596), `2645764e0c28ea9bf9497335555fccb564481ab6` | CEV-Eris icon; inhand sprites by Failed (Discord: greetings_) |
| Food/chilly_pepper_food.rsi | [#7170](https://github.com/Goob-Station/Goob-Station/pull/7170), `6fcbab18016e19efaa7a9754714822e1a0d0d363` | cwestkaemper |
| Food/cotton-pizza-hot.rsi | #7170, same revision | cwestkaemper |
| Food/mothroach_burger.rsi | [#6143](https://github.com/Goob-Station/Goob-Station/pull/6143), `8affab6ead8e89a33a65b573338fe9ba6384663a` | Arraydeess |
| Smokeables/Cigarettes/ecigarette.rsi | [#6634](https://github.com/Goob-Station/Goob-Station/pull/6634), `4b1035f478ba558907d2bddc530eddbfcebee3e9` | Recolor of TG at `bfc9c6ba8126ee8c41564d68c4bfb9ce37faa8f8` |
| Smokeables/Cigarettes/cryostasiscigarette.rsi | #6634, same revision | Recolor of TG at the same revision |

The shaker's referenced primary file is [CEV-Eris drinks.dmi](https://github.com/discordia-space/CEV-Eris/blob/f7aa28fd4b4d0386c3393d829681ebca526f1d2d/icons/obj/drinks.dmi), state `shaker`. That frame has identical visible RGBA pixels to the `shaker` state in [TG drinks.dmi](https://github.com/tgstation/tgstation/blob/bfc9c6ba8126ee8c41564d68c4bfb9ce37faa8f8/icons/obj/drinks.dmi). The two Goob bluespace icon frames preserve its alpha silhouette with altered coloring. This additional primary link is recorded because the referenced historical CEV `COPYING` file describes program licensing without a separate icon notice.

The cigarette recolors trace to the same pinned TG revision: `icons/obj/clothing/masks.dmi` (`cigoff`, `cigon`, `cigbutt`), `icons/mob/clothing/mask.dmi` (`cigoff`, `cigon`), and `icons/mob/inhands/clothing_{left,right}hand.dmi`. The source's item and equipped masks retain the original alpha geometry; inhand art includes adaptations, so it is not claimed to be an identical extraction. The port authors are not substituted for the credited original artists.

The primary [TG README license section](https://github.com/tgstation/tgstation/blob/bfc9c6ba8126ee8c41564d68c4bfb9ce37faa8f8/README.md#license) licenses icons and sounds under Creative Commons Attribution-ShareAlike 3.0 unless otherwise indicated. This matches the imported RSI metadata. These files do not include the source's unrelated balance, chemistry, species, research or gameplay implementations.
