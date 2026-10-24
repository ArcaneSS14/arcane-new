---
name: performance
description: Review hot paths, allocations, iteration, caching, event frequency, and correctness-preserving optimization.
---

# Evaluate execution cost

Identify workload, runtime side and a measured or demonstrably frequent path. Separate server simulation, client CPU, networking and GPU hypotheses. State which work and frequency the proposed change reduces; do not call a heuristic a measured improvement.

Candidates include global scans in frequent events, per-tick allocations/LINQ/closures, repeated stable lookups, dirtying unchanged state, oversized BUI updates, uncontrolled pathfinding, unbatched atmos work, recreated overlays and hot-loop logs. Each needs its actual workload/caller evidence; none is automatically a defect.

Prefer existing queries, dependency caches, buffers, event-driven updates and narrower dirtying when their invalidation/lifetime is correct. Preserve authority, prediction, lifecycle and visual quality. A faster operation that changes the agreed result is a regression.

Use comparable before/after observations, profiling or operation counts proportionate to the claim. An isolated benchmark is not mandatory for every edit; label estimates and unmeasured effects. Choose behavior checks and any broader measurements under [verification](../../rules/verification.md).
