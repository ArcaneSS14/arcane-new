---
name: resources-and-assets
description: Discover and reuse owner resource structure, then validate paths, RSI, audio, maps, attribution, and localization companions.
---

# Resources And Assets

Use the owning resource root. Inspect the existing tree before creating any path. Do not derive directories from repository names, module names, namespaces, or prototype IDs.

Verify the resource owner, the relevant owner-local module or underscore directory, nearby feature files, and exact IDs, keys, paths, states, or collections. Search only the candidate owner path and directly relevant paths; widen only to resolve a demonstrated ownership or collision question.

Add English text for player-visible content. Add Russian counterparts only when explicitly requested. Preserve attribution and asset metadata without editing SPDX.

Do not add repository edit markers inside owner-local module or underscore paths. Add the current repository marker only to inherited text files when ownership requires it and the format supports comments.

Verify RSI metadata, dimensions, states, frame data, case-sensitive paths, audio collections, formats, volume, prediction, and source lifetime.

Use the exact resource paths and specialized validator commands from the current owner workflow. Validate only changed resource types and directly affected paths. Follow root `AGENTS.md`; do not recursively inventory all resources, restore dependencies, or run unrelated builds and validators by default. Run `git diff --check`.
