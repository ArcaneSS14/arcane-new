---
name: prediction
description: Keep responsive shared interactions deterministic and free of duplicated side effects while server authority wins.
---

Prediction provides immediate local feedback while the server remains authoritative.

Predict only short player-driven interactions with locally known inputs and correctable results. Keep hidden information, economy outcomes, permissions, access checks, and persistence authoritative.

Shared predicted code may execute more than once. Separate deterministic state changes from one-shot effects and avoid non-deterministic randomness in predicted paths.

Predicted player feedback requires `en-US` localization; add Russian only when explicitly requested. Resolving the same key twice is acceptable. Showing the feedback twice is not.

Verify repository ownership and edit-marker requirements before changing inherited files.

Build the affected project and choose focused tests for prediction behavior when verification is requested. Use integration coverage when it exercises latency, rejection, repeated input, observers, deletion, or reconciliation. Do not run all root and module integration suites by default; follow root `AGENTS.md`.
