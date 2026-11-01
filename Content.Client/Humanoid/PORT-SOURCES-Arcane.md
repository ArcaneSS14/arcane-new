# Animated marking preview

Behavior ported from Goob-Station/Goob-Station#7459 (AGPL-3.0-or-later),
adapted to Trauma's current organ marking model and picker. No mass marking
resources or replacement UI framework are included. The existing editor layout,
north-facing tiles, colour controls and role/species restrictions are retained.

The animation switch changes display copies only. It neither writes the saved
profile nor sends marking-edit events. Reduced motion stops both tile animations
and the animated variant on the character preview. The catalogue shows one row
for a still/animated pair while keeping existing animated selections editable.
Pairing uses the same explicit/legacy mapping as the organ's wagging action.
