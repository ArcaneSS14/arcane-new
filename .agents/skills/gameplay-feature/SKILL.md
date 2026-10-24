---
name: gameplay-feature
description: Implement complete SS14 features across ownership, ECS, networking, resources, localization, UI, and tests.
---

## Когда применять

Use as routing guidance for a gameplay feature spanning ownership, ECS, authority, networking, UI, resources, localization, or tests.

## До изменения

Map owner-local/inherited paths, marker boundary, assembly/authority, public extension points, prototypes/resources, English locale, UI lifecycle, persistence, and existing owner tests.

## Правила и границы

- A feature is incomplete when changed behavior lacks required owner contracts: server validation, prototypes/assets/UI refresh where applicable, English localization, or existing owner coverage.
- Define data/contracts in the lowest valid layer, implement server authority before presentation, and keep client feedback prediction-safe. Add Russian only on request.
- Use owner roots and existing infrastructure. Mark inherited edits only with the current marker; do not mark owner-local module/underscore paths.
- Do not invent API/prototype/resource IDs, bypass access, use reflection, alter engine, or create parallel test/build/UI infrastructure when no supported extension point exists; stop and report the boundary.

### Minimal examples

Correct: complete one vertical slice through existing authority and presentation owners. Prohibited: create a parallel feature framework. Stop: report an unavailable extension point or owner.

## Выбор подхода

Implement the smallest complete vertical slice through existing owners: authoritative system, allowed Shared contract, client presentation, then only affected prototype/resource/locale/test surfaces.

## Антипаттерны

- Do not add code without its needed data/localization/UI/validation owner surfaces.
- Do not broaden into unrelated feature families, automatic Russian, restore, or full suites.

## Проверка

Choose an affected project/focused owner test and targeted resource validation only for changed surfaces. Report commands actually run; broader CI-equivalent checks require a concrete release/CI risk or request.

## References

- [Feature checklist](references/feature-checklist.md): Read this when planning or reviewing a feature that spans authority, networking, resources, UI, or tests.
