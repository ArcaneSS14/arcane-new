---
name: debugging
description: Reproduce failures, trace authoritative flow, prove the broken assumption, and add targeted regression coverage.
---

Debug before redesigning.

1. Reproduce the exact symptom and record inputs, runtime side, repository owner, and lifecycle stage.
2. Identify the authoritative owner and public entry point.
3. Trace subscription, validation, mutation, dirtying, persistence, and presentation.
4. Compare server and client state for networking issues.
5. Verify every assumed declaration and resource reference.
6. Add temporary diagnostics only around the suspected divergence.
7. Confirm the failing assumption, fix the narrow cause, remove diagnostics, and add a stable regression test.

For localization bugs, inspect English file discovery, keys, variables, selectors, loaded roots, and UI refresh. Compare Russian files only when the issue is Russian-specific or Russian is in scope.

Apply repository markers only to inherited files and never to owner-local module or underscore paths.

After reproducing the issue, build only the affected project and use a focused regression test when verification is requested and the owner can cover the failure. Run an integration test only when it exercises the broken cross-layer path. Follow root `AGENTS.md`; do not restore dependencies or run every affected owner’s full suite by default.
