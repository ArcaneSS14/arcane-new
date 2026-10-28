---
name: prototypes
description: Create, inherit, compose, localize, and validate YAML prototypes in the correct resource root.
---
# prototypes

## Когда применять

Use for prototype creation, review, inheritance or referenced resources.

## До изменения

Find owner root through Modules/CATALOG.md, owner guide and existing Resources; inspect a nearby type and its component schema.
Verify repository identity and edit-marker boundaries before inherited edits.

## Правила и границы

Verify type, ID, parent, component fields, typed IDs and every locale, sprite, sound, dataset and map reference. Prototype fields are configuration; runtime state belongs to systems/components.
Add English display text; change Russian only when explicitly requested.

### Source ledger

Claim: Prototype ownership is selected through the current `Modules/CATALOG.md`, owner guidance, and existing resource root; localization follows the repository's English-source policy.
Source: `Modules/CATALOG.md`, root `AGENTS.md`, applicable owner guide, and the current resource tree.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing catalog routing, owner guidance, resource roots, or localization policy.

### Minimal examples

Correct: verify prototype owner/schema before composing inheritance. Prohibited: add a parallel owner root. Stop: report unknown consumer/schema validation.

## Выбор подхода

Use inheritance for shared defaults, composition for reusable component sets, and a separate prototype for distinct identity.

## Антипаттерны

Do not copy large YAML blocks, duplicate IDs, leave stale references, or use prototype IDs as visible fallback text.

## Проверка

Search duplicate IDs and stale consumers; use an existing targeted validator for changed prototypes and direct resources only.
When lifecycle or map loading changes, choose a focused owner test; do not restore, full-build or run global YAML lint by default.

## References

- [Prototype workflow](references/prototype-workflow.md): Read this when authoring, inheriting, reviewing, or diagnosing a prototype and its referenced resources.
