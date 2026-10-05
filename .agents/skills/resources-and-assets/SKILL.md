---
name: resources-and-assets
description: Discover and reuse owner resource structure, then validate paths, RSI, audio, maps, attribution, and localization companions.
---

# Resources And Assets

Use the owning resource root. Inspect the existing tree before creating any path. Do not derive directories from repository names, module names, namespaces, or prototype IDs.

Verify repository identity, resource owner, owner-local module and underscore directories, nearest feature files, existing IDs, keys, paths, states, and collections.

Add English text and ordered Russian counterparts for player-visible content. Preserve attribution and asset metadata without editing SPDX.

Do not add repository edit markers inside owner-local module or underscore paths. Add the current repository marker only to inherited text files when ownership requires it and the format supports comments.

Verify RSI metadata, dimensions, states, frame data, case-sensitive paths, audio collections, formats, volume, prediction, and source lifetime.

```powershell
Get-ChildItem Resources -Recurse | Select-Object -ExpandProperty FullName
Get-ChildItem Modules -Recurse | Select-Object -ExpandProperty FullName
git grep -n -E "FEATURE_PREFIX|RESOURCE_PATH|LOCALE_KEY" -- Resources Modules Content.*
dotnet restore
dotnet build --configuration DebugOpt --no-restore /m
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
git diff --check
```

Use exact specialized validator commands from current workflows.
