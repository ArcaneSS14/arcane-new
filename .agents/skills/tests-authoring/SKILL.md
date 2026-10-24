---
name: tests-authoring
description: Author deterministic owner-correct tests that prove observable contracts through valid APIs.
---

# Author a regression contract

Use the smallest setup that reaches the production API, event, command, UI message or lifecycle entry responsible for the defect. The assertion must distinguish broken from fixed observable behavior, not merely repeat the implementation or call a different internal handler.

Choose the actual existing test owner under [verification](../../rules/verification.md). Check declarations, access, assemblies and runtime side before using helpers. Do not add reflection, duplicate projects/fixtures/CI targets or runtime-manifest test entries for convenience.

Control simulation time and randomness; avoid wall-clock sleeps, machine culture, live services, test order and leaked global state. Clean up entities, subscriptions, configuration and other state owned by the fixture even on failure.

Choose additional cases from the changed contract: bounds, repeat, denial, deletion, cancellation, reconnect/late state, reference failures or provider differences. They are alternatives based on risk, not a compulsory matrix for every test. Locale assertions follow [locale policy](../../rules/content-and-localization.md).

Run the focused selection and confirm it executed; when feasible demonstrate fails-before/passes-after. State any unperformed half honestly. Build fresh dependencies when necessary and expand checks only under the shared verification policy.
