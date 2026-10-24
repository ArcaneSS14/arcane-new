---
name: resources-and-assets
description: Discover and reuse owner resource structure, then validate paths, RSI, audio, maps, attribution, and localization companions.
---
## Когда применять
Use for resource paths, RSI, audio, maps, attribution or asset consumers.
## До изменения
Find the owner from existing owner paths and nearby assets, never a namespace or prototype ID; identify direct prototype, code, map, locale and visualizer consumers.
## Правила и границы
Check case-sensitive paths, RSI metadata/states, audio format/resources, source provenance and attribution. Keep player-visible companions localized as requested.
Check RSI dimensions, states and frame data; check audio collections, format and volume. Add English player-visible text; change ru-RU only when requested. Apply owner-local/inherited edit-marker rules.
## Выбор подхода
Reuse the owner hierarchy and its supported specialized validation path. Consider prediction, audience and audio lifetime only for an audio task.
## Антипаттерны
Do not create parallel resource trees, change SPDX, infer paths, or validate unrelated resources.
Do not recursively inventory resources.
## Проверка
Verify changed files and direct consumers with the narrowest current validator and final diff.
Validate only the changed resource type and directly affected paths.
## References
- [Asset validation](references/asset-validation.md): Read this when changing an RSI, audio, map asset, attribution, source provenance, or resource path.
