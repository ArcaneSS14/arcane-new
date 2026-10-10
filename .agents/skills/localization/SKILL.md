---
name: localization
description: Maintain English-source localization, adding Russian only when requested, with correct owner paths, variables, selectors, and runtime behavior.
---

Localization is part of feature implementation.

## Mandatory discovery

Before editing:

1. identify the affected `en-US` owner file
2. search exact, old, alternative, and proposed keys
3. identify the existing owner file for the feature
4. inspect `ru-RU` only when requested or when diagnosing a Russian-specific issue
5. inspect code, XAML, prototypes, and maps using the keys

Never invent a path from a repository or module name. Preserve exact underscores, casing, grouping, and relative paths.

## English is the source of truth

English defines key identity, attributes, variables, selectors, file placement, section grouping, and message order.

When English changes:

- leave `ru-RU` untouched by default, including during English additions, removals, renames, moves, or reordering
- update the affected Russian entries only when explicitly requested, preserving the key contract and ordering
- preserve the English contract when editing English
- when Russian is requested, match changed attributes, variables, selectors, and file moves or renames

When Russian is requested, place new entries consistently with the English structure.

A Russian wording-only correction is permitted when explicitly requested.

Do not flag missing or stale Russian entries during English-only work unless they cause the issue being fixed.

## Translation requirements

When Russian is requested, write natural Russian. Do not copy English as a placeholder. Do not use `THE(...)` or equivalent English grammar markers.

Preserve placeholders, markup, line breaks, and control hints required by code.

## Renames and deletions

English remains canonical for translation synchronization, but verify that the English change itself is intentional. Search all references before deleting or renaming a key. Afterward, search for stale variants.

## Module safety

Search root and modules for duplicate keys and relative file-path collisions. A module must not rely on shadowing another owner's FTL file.

## Verification

```powershell
Get-ChildItem Resources/Locale -Recurse -File -Filter *.ftl | Select-Object -ExpandProperty FullName
Get-ChildItem Modules -Recurse -File -Filter *.ftl | Select-Object -ExpandProperty FullName
git grep -n -E "EXACT_KEY|OLD_KEY|PROPOSED_KEY|FEATURE_PREFIX|UI_CONTROL" -- Resources Modules Content.*
git diff -- "*.ftl"
dotnet build --configuration Release --no-restore /p:WarningsAsErrors= /m
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
git diff --check
```
