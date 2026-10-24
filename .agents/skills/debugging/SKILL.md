---
name: debugging
description: Diagnose a concrete failure from primary evidence and select a causal reproduction or regression check.
---

# Diagnose a failure

Start with the supplied primary failure, logs and current path. Record input, runtime side and lifecycle stage only as needed to distinguish causes. Locate the real entry/owner and trace input → validation → mutation → publication → consumer. Resolve a first schema/reference error before downstream cascades.

Compare the implicated runs/versions or states. For desync, compare the same entity on server and client; for UI, follow state generation/application, subscriptions and window lifetime; for locale, check the affected loaded root/key/variables and refresh. Locale scope follows [policy](../../rules/content-and-localization.md).

Use a narrow runtime reproduction, breakpoints, View Variables or temporary diagnostics when current evidence leaves a concrete uncertainty and the environment is available. Added latency is useful for a timing/desync hypothesis, not mandatory for every failure. Reuse existing logs instead of rerunning an expensive command for one known status.

Fix the demonstrated cause using current APIs and the existing owner. Remove temporary diagnostics. Select a focused regression check when it can distinguish the failure; author a stable test only when justified by the changed risk and existing infrastructure. Follow [verification](../../rules/verification.md), not a blanket test/build requirement. Report unavailable evidence and unresolved hypotheses explicitly.
