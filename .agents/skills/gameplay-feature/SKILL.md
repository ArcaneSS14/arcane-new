---
name: gameplay-feature
description: Implement complete SS14 features across ownership, ECS, networking, resources, localization, UI, and tests.
---

Use this as the routing skill for multi-layer work.

Before editing, map repository ownership, owner-local paths, inherited files, edit-marker requirements, assembly layers, authority, prototypes, resources, English locale files, UI lifecycle, persistence, and tests.

A feature is incomplete when code exists but required English localization, prototypes, assets, UI refresh, validation, or owner tests are missing. Russian localization is required only when explicitly requested.

## Implementation order

1. Verify repository identity, APIs, ownership, and extension points.
2. Define data and contracts in the lowest valid assembly.
3. Implement server authority and validation.
4. Implement client presentation and prediction-safe feedback.
5. Add prototypes and resources in the owner root.
6. Add English localization. Add Russian only when explicitly requested, preserving the existing structure and order.
7. Add regression coverage in existing owner tests.
8. Run commands for every changed surface.

Mark inherited edits with the current repository marker. Do not add redundant markers in owner-local module or underscore paths.

Choose a build and focused test for each changed behavior, using the owning projects. Use targeted resource validation for changed prototypes and assets. Follow root `AGENTS.md`; do not restore dependencies or run full root and module suites by default. Run broader CI-equivalent checks only when requested or needed to validate a specific release or CI requirement.
