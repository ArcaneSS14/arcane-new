---
name: networking
description: Implement minimal network events and component state with authority, conversion, dirtying, and regression coverage.
---

Define payloads in Shared and verify current serialization and entity-conversion APIs. Keep payloads minimal. Messages express intent, while the server derives protected results.

Replicate only fields needed by clients. After authoritative mutation, call the correct dirtying path. Predicted state must converge without duplicate popups, sounds, spawns, or resource charges.

Player-facing feedback generated from replicated state requires `en-US` localization; add Russian only when explicitly requested.

Verify repository ownership and edit-marker requirements before modifying inherited files.

Build the affected project and select a focused test for the changed serialization, conversion, authority, dirtying, or prediction behavior when verification is requested. Use integration tests when they cover the actual network path. Follow root `AGENTS.md`; do not restore dependencies or run integration suites for every module by default.
