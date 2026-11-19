# Goob painting sources

The painting implementation and resources are adapted from [Goob-Station/Goob-Station revision cc1d2c911f5cb6c90b36e0899ce918010bd41262](https://github.com/Goob-Station/Goob-Station/tree/cc1d2c911f5cb6c90b36e0899ce918010bd41262). The source code is distributed under [AGPL-3.0-or-later](https://github.com/Goob-Station/Goob-Station/blob/cc1d2c911f5cb6c90b36e0899ce918010bd41262/LICENSES/AGPL-3.0-or-later.txt), as stated in the pinned repository's [license policy](https://github.com/Goob-Station/Goob-Station/blob/cc1d2c911f5cb6c90b36e0899ce918010bd41262/README.md#license).

- Painting, frames, easel, paints, photocopier and history: [Goob #7368](https://github.com/Goob-Station/Goob-Station/pull/7368), credited to Durk (Aidenkrz), including the final source corrections in commits [994fada](https://github.com/Goob-Station/Goob-Station/commit/994fada6849c5b45a951f21e373d238f70f3114e) and [a612555](https://github.com/Goob-Station/Goob-Station/commit/a612555e413e664887e9190f8b8b7f39c287028a).
- Plasmic rainbow crayon and canvas floor: [Goob #6551](https://github.com/Goob-Station/Goob-Station/pull/6551), credited to Dezzzix (NotSoDana).
- Current source entity API corrections: [Goob #7335](https://github.com/Goob-Station/Goob-Station/pull/7335), by madeline-celeste.

Runtime code lives in Arcane's existing shared/server/client projects. It uses Trauma's current item slots, machine parts, power, DoAfter, entity decals and admin-log database. Network validation, copying, cleanup, history selection and asynchronous import checks are adapted to those APIs. Existing object repainting remains separate.

## Artwork

Imported PNG, SVG and generated icon PNG bytes are unchanged from the pinned Goob source. Original metadata content and icon attribution are retained; one trailing blank line in the canvas tile RSI metadata is removed for destination whitespace checks. These licenses remain applicable to the imported artwork.

| Arcane path under Resources/Textures/_Arcane | License and original attribution |
| --- | --- |
| Objects/Fun/canvas.rsi, Objects/Fun/painting_frames.rsi, Structures/Furniture/easel.rsi | CC-BY-SA-3.0; tgstation [artstuff.dmi](https://github.com/tgstation/tgstation/blob/d91390a4474e511279b6ee7525c5982266b9a858/icons/obj/art/artstuff.dmi). Canvas centering is already part of the Goob source. |
| Objects/Fun/paint_bucket.rsi | CC-BY-SA-3.0; tgstation [paint.dmi](https://github.com/tgstation/tgstation/blob/4397d63a55dac7d0536eb9bcc0a0f68634858c50/icons/obj/art/paint.dmi) and [in-hand sprites](https://github.com/tgstation/tgstation/blob/12eed0aa70a932e1ff0474b58525e37372b32c3e/icons/mob/inhands/items_lefthand.dmi). The paint_fill1 derivation is recorded in the original metadata. |
| Objects/Fun/palette.rsi | CC-BY-SA-3.0; tgstation artstuff.dmi above and [palette in-hand sprites](https://github.com/tgstation/tgstation/blob/39308f9dc61c70043781301e096117a7a421dae2/icons/mob/inhands/equipment/palette_lefthand.dmi). |
| Objects/Tiles/canvas.rsi | CC-BY-SA-3.0; NotSoDana, Discord dezzzix, as recorded in the original RSI metadata. |
| Tiles/canvas.png | Canvas floor from Goob #6551 by Dezzzix / NotSoDana, under the pinned Goob repository's default CC-BY-SA-3.0 media policy. |
| Structures/Machines/photocopier.rsi | CC-BY-SA-3.0; Aurorastation [library.dmi](https://github.com/Aurorastation/Aurora.3/blob/8111a8489b1bc5d44ba35dddc344ac7ee7312137/icons/obj/library.dmi). |
| Interface/Paint | MIT; [Tabler Icons v3.49.0](https://github.com/tabler/tabler-icons/tree/v3.49.0/icons) by Paweł Kuna, recoloured white in Goob. Original attributions.yml is preserved and covers the eleven SVG icons and their PNG derivatives. |

The source links and licenses in each original RSI metadata file take precedence for that file. No artwork was relicensed or redrawn for this port.
