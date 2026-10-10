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

```powershell
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
dotnet build Content.MapRenderer/Content.MapRenderer.csproj --configuration DebugOpt --no-restore
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
git diff --check
```

Run the exact current map schema workflow command for changed map files. Inspect the diff for mass reserialization, missing prototypes, and unrelated GUID or metadata changes.
