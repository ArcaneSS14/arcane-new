---
name: audio
description: Add data-driven audio with correct prediction, audience, resources, attribution, and verification.
---

# Audio

Classify the sound before selecting an API: predicted local feedback, PVS world sound, moving entity source, static coordinate source, global notification, client-only UI sound, ambient loop, or music.

## Spatial sources and occlusion

- Decide whether the source follows an entity or stays at a map coordinate. For entity-attached audio, account for entity movement, deletion, parent/container changes, and the lifetime of any returned loop stream. For static audio, verify the intended map/grid coordinates and what happens if that grid moves or is removed.
- Separate audience delivery from audibility: PVS or an explicit player filter selects recipients; spatial distance and occlusion determine what a recipient hears. Check both when choosing playback APIs and range parameters.
- For spatial audio, inspect the listener-to-source occlusion path when walls or other obstacles matter. In this repository, `SharedContentAudioSystem` configures the engine's occlusion ray mask to `CollisionGroup.Impassable`; the client raycasts against that mask and updates audio occlusion. Walls and other physics entities block/attenuate sound only when their collision layer/collider matches that mask. Do not assume every entity, visual wall, or interaction obstruction affects audio.
- Check doors and other dynamic occluders in both relevant states when a feature changes their collision behavior. Review the affected collision layers, collider lifecycle, audio flags, and any content occlusion override. `AudioFlags.NoOcclusion` intentionally bypasses the default raycast; do not set it just to hide an unexplained attenuation issue.
- Global notifications and client-only UI sounds have no world source by default. Do not add entity queries or wall raycasts to them unless the requested behavior is spatial.

Prefer component fields, prototypes, `SoundSpecifier`, and sound collections over hardcoded paths. Keep client-only playback out of Shared dependencies.

Predicted actions must not play the same sound locally and again on authoritative confirmation. Verify source deletion, cancellation, range, audience, occlusion, repetition, and concurrent playback when relevant to the sound type.

Add `en-US` when the audio feature introduces player-visible captions, UI labels, announcements, examine text, or settings. Add `ru-RU` only when explicitly requested.

Preserve source attribution and asset-specific license metadata without editing SPDX.

## Verification commands

Follow root `AGENTS.md` for verification scope. Build the affected project when code changes; validate only changed audio resources with an available targeted validator. Run a focused integration test when predicted or networked playback behavior is in scope and verification is requested. Do not restore or run full builds by default.
