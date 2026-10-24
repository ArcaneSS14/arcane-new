---
name: networking
description: Implement minimal network events and component state with authority, conversion, dirtying, and regression coverage.
---

# State delivery and messages

Trace shared payload/component → sender → transport/entity conversion → receiver/trust checks → authoritative mutation → dirtying/state application → client consumer. Events carry requested intent or notifications; component state carries a replicated snapshot. Choose the actual mechanism rather than treating them interchangeably.

Use supported serializable types and current NetEntity conversion APIs. Keep payloads small and reveal only data the viewer may know; a limited response/BUI state is preferable to exposing a complete protected component. Client-provided prices, permissions, identities and outcomes are not authority: follow [security](../security-and-validation/SKILL.md).

After authoritative mutation use the owning component's correct dirtying path. Field-level dirtying requires generated support and a meaningful update reduction. Inspect both sender and receiver when payload shape/types change; keep old/new message expectations compatible with the task's supported versions.

Select missing/deleted entity, stale/rejected/late state or reconnect checks according to the changed path under [verification](../../rules/verification.md). Use [prediction](../prediction/SKILL.md) for replay and one-shot effects, [PVS](../pvs/SKILL.md) for interest/privacy, and [localization-in-code](../localization-in-code/SKILL.md) for typed presentation. None requires loading the others when its contract is unchanged.
