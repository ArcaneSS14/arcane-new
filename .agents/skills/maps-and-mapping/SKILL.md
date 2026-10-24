---
name: maps-and-mapping
description: Edit maps, map prototypes, grids, placements, entity references, and serialized compatibility safely.
---

# Serialized maps

Choose the actual owner/consumer: map file, map prototype, template or runtime spawner. Prefer the supported editor/serializer. Verify changed prototype/tile/field references and loaded resources; source-fork chunks may require deliberate schema/ID adaptation.

Inspect relevant parents, grids, coordinate/rotation, anchoring, containers, map/entity IDs and reference compatibility. Keep intended additions/removals distinct from serializer-wide reordering, GUID or metadata churn. A small behavior change must not silently reserialize unrelated map content.

Use [resource rights](../../rules/third-party-materials.md) for imports and [locale policy](../../rules/content-and-localization.md) for visible labels. Sprite/map conflicts go to the user under [merge policy](../../rules/merge-conflict-resolution.md).

Load/validate the affected map through the actual current owner/workflow under [verification](../../rules/verification.md); establish whether a specialized command applies before using it. Build code only when required by the changed executable contract. Inspect the final map diff for missing prototypes and unrelated churn.
