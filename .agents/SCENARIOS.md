# Combined routes

Use [CATALOG.md](CATALOG.md) for an ordinary single-owner change. This file adds combinations; it does not create another policy or mandatory all-layer preflight.

## Cross-layer feature

Establish the requested outcome and changed contracts with [ai-workflow](skills/ai-workflow/SKILL.md). Follow [architecture](rules/architecture-and-ownership.md) and the relevant owner guide through [Modules/CATALOG.md](../Modules/CATALOG.md).

Select only affected surfaces: ECS state/execution, authority and transport, resources, UI, localization, persistence and tests. Trace the feature's real input → validation → state change → publication → consumer. Check that every changed producer has its required consumer and cleanup; a feature need not introduce all these layers.

## Inherited change or actual conflict

Read [trajectory](rules/fork-trajectory-priority.md) and [markers](rules/arcane-edit-markers.md). For synchronization add [upstream-maintenance](skills/upstream-maintenance/SKILL.md); for an actual conflict add [merge policy](rules/merge-conflict-resolution.md). Use git-workflow only if history/staging operations are part of the task. Sprite/map conflicts go to the user.

## Port, restoration or unexplained removal

Use [porting](skills/porting/SKILL.md), [history](rules/change-history-analysis.md), [destination](rules/port-destination.md) and [third-party rights](rules/third-party-materials.md), then only the affected domain skills. A specified historical ref remains the source contract; later fixes are reported separately rather than silently widening it.

## UI with server state

Choose [BUI](skills/bound-user-interface/SKILL.md) for entity-owned or [EUI](skills/eui/SKILL.md) for session-owned transport. Add [XAML](skills/xaml-ui/SKILL.md) for view changes, [forms](skills/forms-and-input-validation/SKILL.md) for parsing, [security](skills/security-and-validation/SKILL.md) for trust, and [localization-in-code](skills/localization-in-code/SKILL.md) for resolved text, as applicable.

## Resources

Choose prototype, map, asset or schema skills from CATALOG by the actual loader. Add the relevant audio/appearance/render route only when that contract changes. Locale selection follows [content policy](rules/content-and-localization.md).

## Build or assembly change

Use [module-architecture](skills/module-architecture/SKILL.md) for access/dependency decisions and [build-and-packaging](skills/build-and-packaging/SKILL.md) for consumers and outputs. Confirm the affected graph and existing infrastructure, not every project.

## Review or failure

For review use [code-review](skills/code-review/SKILL.md) and applicable public criteria. For an identified failure use [debugging](skills/debugging/SKILL.md), then choose a causal check under [verification](rules/verification.md). A CI-status question or Markdown edit does not require gameplay bootstrap.
