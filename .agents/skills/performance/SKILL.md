---
name: performance
description: Review hot paths, allocations, iteration, caching, event frequency, and correctness-preserving optimization.
---

## Когда применять

Use only for a measured issue or a clearly high-frequency path such as `Update`, a broad query, replication/dirtying, pathfinding, atmos, or client rendering.

## До изменения

Establish the owner, frequency, baseline or operation count, allocation/query/dirtying/network/render cost, and correctness contract. Inspect the actual affected path for broad queries, LINQ, closures, allocations, repeated prototype/localization lookup, dictionary creation, networking frequency, dirtying, pathfinding, atmos work, and render allocations.

## Правила и границы

- Prefer existing bounded ECS queries, owner caches/dependencies, reused buffers, event-driven updates, and narrow dirtying.
- Do not cache data without a defined owner, lifetime, and invalidation strategy.
- Preserve observable behavior, prediction, authority, and lifecycle semantics. A micro-optimization must not change correctness or remove required work.
- Compare before/after with profiler or benchmark evidence, a focused test, or a reasoned operation count when tooling is not appropriate. Report measured improvement separately from unverified risks.

## Выбор подхода

First remove unnecessary frequency or scope; then reuse an established buffer/cache; only then consider an algorithmic change. Choose event-driven work over repeated polling when the owner already exposes the needed event, and narrower dirtying over a larger replicated update when the contract permits it.

## Антипаттерны

- Do not optimize from intuition, create a cache without invalidation, or replace a bounded owner query with a global scan.
- Do not use allocation/LINQ/closure changes as a pretext to alter prediction, authority, lifecycle, or visible behavior.
- Do not state a performance gain without measurement or an explicit reasoned operation-count comparison.

## Проверка

Record the measured baseline and after result, or the before/after operation count, plus correctness coverage for the affected contract. State unmeasured risks separately; do not run a full suite or benchmark project unless the task requires it.

## References

- [Common performance pitfalls](references/common-performance-pitfalls.md): Read this when changing a hot update path, query, allocation pattern, replicated state frequency, or client visual update loop.
