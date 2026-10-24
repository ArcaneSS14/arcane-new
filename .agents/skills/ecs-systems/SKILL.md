---
name: ecs-systems
description: Implement entity behavior, subscriptions, lifecycle handling, and reusable action APIs.
---

# ECS behavior and interactions

Systems own behavior and mutation; components hold state. Use the highest-level existing owner API that expresses the operation. Resolve dependencies through the established injection pattern and subscribe in Initialize; keep event handlers thin.

## Unfamiliar subsystem, when needed

Find the primary component/prototype, relevant subscriptions and real player/simulation entry. Trace validation → mutation → dirtying → consumer and the assemblies on those arrows. Identify lifecycle and existing focused tests only as relevant. Exact component/event/prototype/key searches reveal different parts of the same feature; do not copy a nearby system before understanding its ownership.

## Reusable action path

OnEvent handles routing/handled/cancelled flags → TryAction validates its own request → quiet CanAction checks without mutation → one execution path applies effects. UI/verb early checks are feedback, never a substitute for authoritative checks in Try. Can must not spend, spawn or change components; quiet probes must not spam feedback. Set routing flags only when this system consumed the action.

Keep a simple boolean result when sufficient; introduce a detailed result only for real callers needing it. Timed/granted lifecycle belongs to [actions-and-doafter](../actions-and-doafter/SKILL.md).

Use current TryComp guards, Resolve for optional Entity<T?> inputs and EnsureComp for idempotent creation where their contract fits. Prefer established queries over global scans; verify signatures after API changes. Use deferred deletion/removal when dispatch or iteration would otherwise be invalidated.

Choose startup/map-init/shutdown hooks by dependency validity and safe cleanup. Handle predicted re-entry, external callbacks and cached entity references according to lifetime; see [entity lifecycle](../entity-lifecycle-and-spawning/SKILL.md). Frequent Update/event allocations, broad queries and lookups are hypotheses for [performance](../performance/SKILL.md), not universal proof of a bug. Select changed interaction/lifecycle checks under [verification](../../rules/verification.md).
