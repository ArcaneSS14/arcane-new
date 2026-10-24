---
name: npc-ai
description: Modify NPC behavior, HTN tasks, navigation, steering, prototypes, and AI debugging.
---

Trace the current AI stack before adding a new behavior.

Identify:

- the owning server system;
- HTN operator, task, or utility selection;
- target acquisition;
- pathfinding and steering;
- cooldown and cancellation;
- prototype configuration;
- debug tools and tests.

Avoid expensive global searches in repeated updates. Keep behavior deterministic where tests require it. Cancel or invalidate navigation when entities, maps, targets, or goals change.

Update affected prototypes and select checks for observable changes under [verification](../../rules/verification.md). No-target, unreachable-target, deletion, map transfer or repeated planning matter when that contract changes; prototype tuning does not automatically require new integration coverage.
