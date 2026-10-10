---
name: prototypes
description: Create, inherit, compose, localize, and validate YAML prototypes in the correct resource root.
---

## Workflow

1. Verify repository identity, resource ownership, and edit-marker boundaries.
2. Find a current nearby prototype of the same type and owner.
3. Confirm the owning resource root from `module.yml`.
4. Verify fields against the destination component schema.
5. Prefer inheritance or composition over copying a large prototype.
6. Use stable specific IDs and typed IDs in code.
7. Add English display text; add or update Russian only when explicitly requested.
8. Verify every referenced prototype, sprite, state, sound, dataset, map, and locale key.
9. Search for duplicate IDs and stale references.

Do not place repository-owned prototypes in another owner's root for convenience. Do not expose prototype IDs as fallback player text.

When Russian prototype localization is explicitly requested, preserve matching keys, attributes, variables, selectors, paths, and ordering.

Validate only the changed prototype and directly referenced resources with an available targeted validator. Build the affected project when code changes. Run a focused owner test when lifecycle or map-loading behavior is in scope and verification is requested. Follow root `AGENTS.md`; do not restore dependencies, run a full build, or run the global YAML linter by default.

When verification is requested and the prototype changes server/client lifecycle or map loading, select a focused owner test that covers that path. Do not run the complete integration project by default.
