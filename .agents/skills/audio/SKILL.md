---
name: audio
description: Add data-driven audio with correct prediction, audience, resources, attribution, and verification.
---

## Когда применять

Use for predicted local feedback, PVS world sound, entity-attached or coordinate sound, global notifications, UI-only sound, ambient loops, or music.

## До изменения

Classify the sound and identify its audience separately from its audible spatial behavior. Decide whether its source follows an entity or remains at a static coordinate, then trace transform, parent/container, map-transfer, cancellation, and deletion lifetime. Locate the existing `SoundSpecifier`, prototype field, or sound collection before adding a resource path.

## Правила и границы

- PVS or an explicit player filter selects recipients; range, spatial position, and occlusion determine what a recipient hears. Verify both rather than treating audience delivery as audibility.
- Choose an entity source when sound must follow that entity's transform and lifetime. Choose a coordinate source only when the coordinate's map/grid lifetime is correct, including grid movement or removal.
- For spatial sound, verify range, PVS recipients, player filter, occlusion, and relevant collision layers. `AudioFlags.NoOcclusion` deliberately bypasses default occlusion and is appropriate only when that behavior is required, not to hide an unexplained attenuation issue.
- `SharedContentAudioSystem` configures the engine occlusion ray mask as `CollisionGroup.Impassable`; the client raycasts against that mask when updating audio occlusion. A wall or physics entity blocks/attenuates sound only when its collider/layer participates in that mask. Check dynamic occluders, such as doors, in every affected collision state.
- Prefer component fields, prototypes, `SoundSpecifier`, and existing sound collections over hardcoded paths. Keep client-only playback out of Shared dependencies.
- A predicted action must not play the same sound again on authoritative confirmation. For every loop, define the stream owner and stop/clean it up on cancellation, deletion, source removal, and map transfer.
- Preserve source attribution and asset metadata; do not edit SPDX. Add localization only for genuinely player-visible captions, UI, or settings, with `ru-RU` only on explicit request.

## Выбор подхода

Use predicted local audio for immediate correctable feedback, PVS world delivery for audible in-world events, an entity-attached source for moving lifetime-bound sound, and a static coordinate source for a stable location. Use global notification or UI-only audio only when it intentionally has no world source; do not add entity queries or wall raycasts to those categories without a spatial requirement.

## Антипаттерны

- Do not hardcode a path when an existing specifier, prototype field, or collection is the owner.
- Do not duplicate predicted and authoritative playback, leak a loop after its owner ends, or apply `NoOcclusion` without the required behavior.
- Do not assume every visual wall or interaction obstruction occludes audio; verify the relevant collision/occlusion path.

## Проверка

Check source deletion, cancellation, source removal, map transfer, range, audience, occlusion, repetition, and concurrent playback as relevant. Verify player-visible captions/UI/settings only when they are in scope. Follow root `AGENTS.md`: use an affected-project build, targeted resource validator, or focused integration coverage only when code/resources or the requested behavior requires it; do not restore or run full builds by default.

## References

No reference is required for ordinary audio routing; use the owner-local implementation and prototype fields when selecting playback, audience, and loop lifetime.
