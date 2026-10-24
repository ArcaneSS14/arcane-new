# Rule and skill routing

Start with [root guidance](../AGENTS.md). This is the index for changed surfaces; [SCENARIOS.md](SCENARIOS.md) combines routes when concerns intersect. Rule paths below are relative to .agents/; skill entries resolve to skills/<name>/SKILL.md. Load only entries relevant to the task.

## Policy by situation

| Situation | Public owner |
| --- | --- |
| Root Content.* owner/project | [Modules/CATALOG.md](../Modules/CATALOG.md) and its relevant guide |
| Placement, layers, project references, inaccessible API | [architecture-and-ownership](rules/architecture-and-ownership.md), [coding-and-api-design](rules/coding-and-api-design.md) |
| Inherited path or upstream sync | [fork-trajectory-priority](rules/fork-trajectory-priority.md), [arcane-edit-markers](rules/arcane-edit-markers.md) |
| Port, restoration, revert or unexplained removal | [change-history-analysis](rules/change-history-analysis.md), [port-destination](rules/port-destination.md) |
| Actual conflict | [merge-conflict-resolution](rules/merge-conflict-resolution.md) |
| C# / ECS / comments / prototype YAML | Matching [C#](rules/csharp-writing-conventions.md), [ECS](rules/ecs-writing-conventions.md), [comments](rules/commenting-conventions.md) or [YAML](rules/yaml-prototype-conventions.md) convention |
| Naming or compatibility | [naming-and-compatibility](rules/naming-and-compatibility.md) |
| Player text / locale | [content-and-localization](rules/content-and-localization.md) |
| External material | [third-party-materials](rules/third-party-materials.md) |
| Check / Git action / completion | [verification](rules/verification.md), [git-safety](rules/git-safety.md), [review-and-handoff](rules/review-and-handoff.md), as applicable |

## Skills by changed surface

| Surface | Skills (choose the affected concern) |
| --- | --- |
| C# / assembly design | [csharp-style](skills/csharp-style/SKILL.md), [module-architecture](skills/module-architecture/SKILL.md) |
| ECS state / events / execution / format | [ecs-components](skills/ecs-components/SKILL.md), [ecs-events](skills/ecs-events/SKILL.md), [ecs-systems](skills/ecs-systems/SKILL.md), [serialization-and-datafields](skills/serialization-and-datafields/SKILL.md) |
| Spawn/delete/replace or links | [entity-lifecycle-and-spawning](skills/entity-lifecycle-and-spawning/SKILL.md), [entity-relations-and-links](skills/entity-relations-and-links/SKILL.md) |
| State transport / replay / interest | [networking](skills/networking/SKILL.md), [prediction](skills/prediction/SKILL.md), [pvs](skills/pvs/SKILL.md) |
| Verbs/in-hand interactions / granted or timed actions | [ecs-systems](skills/ecs-systems/SKILL.md), [actions-and-doafter](skills/actions-and-doafter/SKILL.md) |
| Item movement / coordinates and collision / deferred work | [containers-and-inventory](skills/containers-and-inventory/SKILL.md), [transform-and-physics](skills/transform-and-physics/SKILL.md), [timers-and-async](skills/timers-and-async/SKILL.md) |
| Damage/effects / round / mind/role / NPC | [damage-status-and-effects](skills/damage-status-and-effects/SKILL.md), [round-and-game-rules](skills/round-and-game-rules/SKILL.md), [minds-roles-and-objectives](skills/minds-roles-and-objectives/SKILL.md), [npc-ai](skills/npc-ai/SKILL.md) |
| Commands/config / privileged actions / DB / files / external service | [commands-and-cvars](skills/commands-and-cvars/SKILL.md), [admin-and-permissions](skills/admin-and-permissions/SKILL.md), [database-migrations](skills/database-migrations/SKILL.md), [save-data-and-configuration](skills/save-data-and-configuration/SKILL.md), [external-services](skills/external-services/SKILL.md) |
| Construction / chemistry / atmos / datasets / random choice | [construction-and-machines](skills/construction-and-machines/SKILL.md), [chemistry-and-reagents](skills/chemistry-and-reagents/SKILL.md), [atmos](skills/atmos/SKILL.md), [collections-and-datasets](skills/collections-and-datasets/SKILL.md), [randomness-and-determinism](skills/randomness-and-determinism/SKILL.md) |
| View / entity UI / session UI / input syntax | [xaml-ui](skills/xaml-ui/SKILL.md), [bound-user-interface](skills/bound-user-interface/SKILL.md), [eui](skills/eui/SKILL.md), [forms-and-input-validation](skills/forms-and-input-validation/SKILL.md) |
| FTL (including prototype text) / code-resolved text | [localization](skills/localization/SKILL.md), [localization-in-code](skills/localization-in-code/SKILL.md) |
| Prototypes / other YAML / maps / asset ownership | [prototypes](skills/prototypes/SKILL.md), [yaml-and-schema](skills/yaml-and-schema/SKILL.md), [maps-and-mapping](skills/maps-and-mapping/SKILL.md), [resources-and-assets](skills/resources-and-assets/SKILL.md) |
| Appearance data / render resources / sound | [appearance-and-visualizers](skills/appearance-and-visualizers/SKILL.md), [sprite-overlays-and-shaders](skills/sprite-overlays-and-shaders/SKILL.md), [audio](skills/audio/SKILL.md) |
| Trust / logs / execution cost | [security-and-validation](skills/security-and-validation/SKILL.md), [logging-and-errors](skills/logging-and-errors/SKILL.md), [performance](skills/performance/SKILL.md) |
| Build graph, CI, output/package | [build-and-packaging](skills/build-and-packaging/SKILL.md); module-architecture only for affected references/ownership |
| Author tests / choose and run checks / diagnose a failure | [tests-authoring](skills/tests-authoring/SKILL.md), [testing](skills/testing/SKILL.md), [debugging](skills/debugging/SKILL.md) |
| Technical docs / Git stage or history / multi-step task | [documentation](skills/documentation/SKILL.md), [git-workflow](skills/git-workflow/SKILL.md), [ai-workflow](skills/ai-workflow/SKILL.md) |
| Feature port / upstream integration | [porting](skills/porting/SKILL.md), [upstream-maintenance](skills/upstream-maintenance/SKILL.md), plus affected domains |
| Review | [code-review](skills/code-review/SKILL.md), plus changed-surface policy and contracts; implementation commands require a separate implementation task |
