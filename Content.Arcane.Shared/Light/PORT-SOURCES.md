# Character lights

Adapted from [Goob #7351](https://github.com/Goob-Station/Goob-Station/pull/7351)
and the resulting `Content.Goobstation.Shared/Light` code at
`45e8d6b1cb1d3464762c8a50ce2f7f5ddf6e59c1`, under AGPL-3.0-or-later.

The port obtains the skin colour from Trauma's current visual torso, coalesces
profile/organ/inventory updates, and uses the existing point-light and toggle
visual systems. Helmet power remains owned by the existing handheld-light and
battery systems; there is no replaceable cell or self-recharge workaround.
