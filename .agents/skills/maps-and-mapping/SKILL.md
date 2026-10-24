---
name: maps-and-mapping
description: Edit maps, map prototypes, grids, placements, entity references, and serialized compatibility safely.
---
# maps-and-mapping

## Когда применять

Use for map files, map prototypes, templates or runtime spawners.

## До изменения

Choose the correct representation; inspect grids, coordinates, parents, containers, anchoring, entity references, prototypes, tiles and runtime dependencies.
Also verify component fields and map IDs. Add English localization for player-visible map text; change ru-RU only when requested.

## Правила и границы

Maps are serialized compatibility artifacts. Use supported editor/serializer paths and preserve existing map/resource ownership.

### Source ledger

Claim: Player-visible map text follows the repository English-source localization policy; Russian map text is opt-in.
Source: Root `AGENTS.md` and `.agents/rules/content-and-localization.md`.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing localization policy or map/resource ownership conventions.

### Minimal examples

Correct: use the existing map/prototype/template owner and validate serialized references. Prohibited: accept serializer churn unrelated to the edit. Stop: report unknown map/prototype compatibility.

## Выбор подхода

Use a map for fixed placement, map prototype/template for reusable serialized layout, and runtime spawner only for dynamic behavior.

## Антипаттерны

Do not manually cause mass reserialization, GUID/metadata churn, invalid parent/container links or foreign-fork chunk copying.

## Проверка

Run the exact current map validator only for a changed map; inspect diff for placement and serializer noise.
Check missing prototypes, unrelated GUID/metadata changes and serializer churn. Do not restore, full-build or run global lint by default.

## References

- [Map change review](references/map-change-review.md): Read this when reviewing a map diff, serializer output, placement change, or map-resource dependency.
