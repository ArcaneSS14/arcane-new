---
name: prototype-localization
description: Maintain English prototype localization and add Russian localization only when explicitly requested.
---

Prototype localization is part of the prototype contract.

1. Identify localization conventions for the prototype type.
2. Inspect the owning resource root and affected English file. Inspect Russian only when requested or diagnosing a Russian-specific issue.
3. Search prototype IDs, current keys, alternative keys, and nearby family keys.
4. Use the existing English owner file; use its Russian counterpart only when Russian is in scope.
5. Treat English as the structural source of truth.
6. Change Russian entries only when explicitly requested; preserve matching keys, variables, selectors, and ordering when doing so.
7. Preserve variables, selectors, attributes, and literal semantic data.
8. Search stale references after ID or key changes.

When Russian localization is requested, place entries consistently with the English structure.

Prototype IDs are machine identifiers and must not leak as display text. When Russian is requested, use natural wording and do not use `THE(...)` wrappers.

Run Release resource validation and inspect missing-key output.
