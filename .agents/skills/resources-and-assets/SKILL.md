---
name: resources-and-assets
description: Discover and reuse owner resource structure, then validate paths, RSI, audio, maps, attribution, and localization companions.
---
# resources-and-assets

## Когда применять

Use for resource paths, RSI, audio, maps, attribution or asset consumers.
Use for resource paths, RSI, audio, maps, attribution or asset consumers.

## До изменения

Find the owner from existing owner paths and nearby assets, never a namespace or prototype ID; identify direct prototype, code, map, locale and visualizer consumers.
Find the owner from existing owner paths and nearby assets, never a namespace or prototype ID; identify direct prototype, code, map, locale and visualizer consumers.

## Правила и границы

Check case-sensitive paths, RSI metadata/states, audio format/resources, source provenance and attribution. Keep player-visible companions localized as requested.
Check RSI dimensions, states and frame data; check audio collections, format and volume. Add English player-visible text; change ru-RU only when requested. Apply owner-local/inherited edit-marker rules.

### Source ledger

Claim: Asset companions use the repository English-source localization policy, and inherited versus owner-local asset edits follow Arcane marker policy.
Source: Root `AGENTS.md`, `.agents/rules/content-and-localization.md`, and `.agents/rules/arcane-edit-markers.md`.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing localization, asset ownership, or edit-marker policy.

### Minimal examples

Correct: reuse owner-local RSI/audio resource structure and validate direct paths. Prohibited: create a parallel hierarchy. Stop: report unknown asset provenance/consumer.

## Выбор подхода

Reuse the owner hierarchy and its supported specialized validation path. Consider prediction, audience and audio lifetime only for an audio task.
Reuse the owner hierarchy and its supported specialized validation path. Consider prediction, audience and audio lifetime only for an audio task.

## Антипаттерны

Do not create parallel resource trees, change SPDX, infer paths, or validate unrelated resources.
Do not recursively inventory resources.
Do not recursively inventory resources.

## Проверка

Verify changed files and direct consumers with the narrowest current validator and final diff.
Validate only the changed resource type and directly affected paths.
Validate only the changed resource type and directly affected paths.

## References

- [Asset validation](references/asset-validation.md): Read this when changing an RSI, audio, map asset, attribution, source provenance, or resource path.
