---
name: yaml-and-schema
description: Author and diagnose YAML, prototype schemas, workflows, manifests, and structured configuration with exact validation.
---
# yaml-and-schema

## Когда применять

Use for YAML whose consumer and schema must be identified.
Use for YAML whose consumer and schema must be identified.

## До изменения

Identify the real consumer: prototype loader, map serializer, workflow, manifest or custom configuration; inspect a current nearby file.
Verify repository identity and edit-marker boundaries before inherited edits.
Verify repository identity and edit-marker boundaries before inherited edits.

## Правила и границы

Check schema, scalar types, nullability, quoting, list/map shape, required fields and duplicate keys, not YAML syntax alone. Preserve unrelated formatting and ordering.
Common failures include unknown component fields, number/boolean strings, invalid workflow quoting, frontmatter not first, obsolete field names and files outside the owner root.
Common failures include unknown component fields, number/boolean strings, invalid workflow quoting, frontmatter not first, obsolete field names and files outside the owner root.

### Minimal examples

Correct: verify schema consumer and exact field type before YAML edit. Prohibited: quote a number/boolean as a string. Stop: report unknown schema/owner-root validation.

## Выбор подхода

Use the destination consumer's established structure and validator; build code only when code changed or that validator requires binaries.
Use the destination consumer's established structure and validator; build code only when code changed or that validator requires binaries.

## Антипаттерны

Do not guess validators, copy a module path from another fork, or run generic/full builds for YAML-only work.
Do not guess validators, copy a module path from another fork, or run generic/full builds for YAML-only work.

## Проверка

Run only the exact current specialized validator when it exists; inspect IDs, paths and references in the final diff.
Do not restore, full-build or run global lint by default.
Do not restore, full-build or run global lint by default.

## References

No reference is required for ordinary schema selection.
