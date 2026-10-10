
Store content in the resource root declared by its owner. Base resources belong in root `Resources`. Module resources belong in that module.

## Player-visible text

All player-visible text MUST use localization. Do not expose raw enum names, prototype IDs, component names, exception messages, or internal validation details.

## Discover before placement

Before editing localization:

1. inspect only the affected owner-local `en-US` file
2. search exact, old, alternative, and proposed keys
3. identify the existing file for the same UI, system, prototype family, or feature
4. inspect `ru-RU` only when Russian is explicitly requested or the issue is Russian-specific
5. create a file only when no existing file is a valid owner

Preserve exact directories, leading underscores, casing, grouping, and relative paths. Never derive a path from a module name without inspecting the repository.

## English is structurally canonical

`en-US` defines:

- the key set
- key names
- message order
- section order
- required attributes
- variable names
- selector structure
- relative file paths

By default, update `en-US` only. Do not create, change, remove, reorder, or synchronize `ru-RU` entries unless the user explicitly requests Russian localization.

When Russian is requested, preserve its key contract and place entries consistently with the English structure.

Removing, renaming, moving, or reordering English localization does not require an automatic Russian edit. Search code, XAML, prototypes, maps, and the affected English file for stale references after renames or removals.

A Russian-only wording fix is in scope only when the user explicitly requests Russian localization.

Do not treat missing or stale Russian entries as defects during English-only work unless they cause the reported issue.

## Translation quality

When Russian is requested, write natural Russian for the actual context. Preserve meaning, variables, markup, and control hints rather than English word order.

When Russian is requested, do not add `THE(...)`, article wrappers, or equivalent English grammar markers to Russian strings.

Check long Russian text in constrained UI controls when Russian is in scope. Fix layout rather than silently removing meaning.

## Module paths and duplicates

Before creating a module FTL file, search root resources and every module for the same relative path. A module MUST NOT depend on shadowing a core or foreign-module file.

Search keys globally. File separation does not make duplicate FTL keys safe.

## Runtime culture behavior

Do not store localized output in persistent, network, or authoritative state. Resolve text at the presentation boundary. Long-lived controls must refresh on culture changes and clean up subscriptions.

## Verification

```powershell
Get-ChildItem Resources/Locale -Recurse -File -Filter *.ftl | Select-Object -ExpandProperty FullName
Get-ChildItem Modules -Recurse -File -Filter *.ftl | Select-Object -ExpandProperty FullName
git grep -n -E "EXACT_KEY|OLD_KEY|PROPOSED_KEY|FEATURE_PREFIX" -- Resources Modules Content.*
git diff -- "*.ftl"
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
git diff --check
```
