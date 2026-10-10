---
name: maps-and-mapping
description: Edit maps, map prototypes, grids, placements, entity references, and serialized compatibility safely.
---

Maps are serialized compatibility artifacts, not ordinary hand-written YAML.

## Workflow

1. Decide whether the change belongs in a map file, map prototype, template resource, or runtime spawner.
2. Use the supported editor or serializer path where possible.
3. Verify every prototype, tile, component field, resource, and required runtime module.
4. Review parents, grids, coordinates, anchored state, containers, map IDs, and entity references.
5. Avoid unrelated serializer churn.
6. Add `en-US` for player-visible map, landmark, device, or UI text. Add `ru-RU` only when explicitly requested.
7. Load or validate the affected map.

Do not copy map chunks from another fork before resolving unavailable prototypes and changed schemas deliberately.

## Verification commands

Run the exact current map schema workflow command only for changed map files. Build the affected project if code changed, and inspect the diff for mass reserialization, missing prototypes, or unrelated GUID and metadata changes. Follow root `AGENTS.md`; do not restore dependencies or run full builds and global linters by default.

Run the exact current map schema workflow command for changed map files. Inspect the diff for mass reserialization, missing prototypes, and unrelated GUID or metadata changes.
