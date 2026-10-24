---
name: naming-conventions
description: Name C# symbols, events, prototypes, FTL keys, resources, and compatibility surfaces consistently.
---

# naming-conventions

## Когда применять

Use when naming or renaming symbols, events, prototypes, FTL keys, resources, CVars, network contracts, or serialized compatibility identifiers.

## До изменения

Find owner conventions, exact declarations, consumers, compatibility surface, locale/resource root, and applicable fork/partial naming boundary before selecting a name.

## Правила и границы

- Names communicate owner, timing, and intent. Use repository casing/suffixes and established vocabulary; events state attempt/pre-change/notification/completion semantics.
- Prototype IDs are stable machine identifiers, FTL keys are lowercase kebab-case with clear owner/feature prefix, and variables describe meaning rather than UI position. File placement does not prevent duplicate keys.
- Fork-suffixed partials follow current owner/file-family suffix and only extend an eligible same-assembly type; do not rename existing partials to foreign suffixes. Prefer a compatible partial only when supported.
- Before renaming serialized fields, prototype IDs, map entities, database fields, CVars, locale keys, or network messages, search relevant consumers and provide migration/compatibility alias when required. Russian is in scope only on request.
- Stop without a supported extension point: do not bypass access, reflection, engine edits, or parallel identifiers to avoid a stable rename/migration.

### Minimal examples

Correct: preserve a stable prototype/FTL/CVar identifier and search consumers before rename. Prohibited: use display text as a machine ID. Stop: report unproven compatibility/migration path.

## Выбор подхода

Choose stable owner-prefixed identifiers for compatibility surfaces and existing partial/resource naming for conflict-sensitive paths. Search every reference only for a public compatibility rename or explicit all-reference task.

## Антипаттерны

- Do not invent abbreviations, use display text as an ID, or rename stable contracts without consumer/migration analysis.
- Do not use a foreign fork suffix or a partial to bypass assembly/access constraints.

## Проверка

For a rename, use targeted consumer search and affected build/resource validation only when that compatibility surface changed; do not run Release/global lint by default.

## References

- [C# and event naming](references/csharp-and-event-naming.md): Read this when naming a C# method, event, action, or API whose mutation and cancellation semantics must be clear.
- [Resource and locale naming](references/resource-and-locale-naming.md): Read this when naming an FTL key, resource path, sprite state, audio collection, or other content identifier.
