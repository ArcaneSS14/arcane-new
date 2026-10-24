---
name: csharp-style
description: Write repository-consistent C# only after proving symbol access, ownership, and compatibility.
---

# C# in the current owner

Style does not override correctness or access. Use [C# conventions](../../rules/csharp-writing-conventions.md), [ECS conventions](../../rules/ecs-writing-conventions.md), [comments](../../rules/commenting-conventions.md) and [naming/compatibility](../../rules/naming-and-compatibility.md) for house rules; [API/access](../../rules/coding-and-api-design.md) owns modifiers and assembly boundaries.

Verify unfamiliar declarations actually called or changed. A remembered helper name, namespace or project reference does not prove accessibility. Prefer existing owner APIs, guard clauses and explicit state transitions; early returns must preserve cleanup. Use nullable annotations and prove any null-forgiving invariant locally.

Type patterns inspect an object already in hand, e.g. `component is TransformComponent transform`. An EntityUid is an identifier, not a component object: look up its component through the current EntitySystem API, e.g. TryComp, with its actual signature. A type test cannot replace ECS component lookup.

For dependency injection read [dependency-injection](references/dependency-injection.md) only when selecting an owner/type or matching injection shape. Choose collections by semantics; use established queries/reused buffers and account for capture cost only on a relevant frequent path. Performance hypotheses belong to [performance](../performance/SKILL.md).

## Version-sensitive work

Read MSBuild/Content.props and the owning project for effective LangVersion/TFM/Nullable rather than assuming every test/tool project shares defaults. The current baseline is C# 14/net10.0; it does not require adopting every new syntax feature.

When touching field-backed accessors, serialization attributes or extension declarations, check the bound member and attribute target. For span-sensitive overloads/expression trees, check the actual selected overload and provider-supported expression; neither syntax modernization nor compilation alone proves provider behavior. Do not introduce uncommon operators/partials just to demonstrate a language version.

## Conditional examples

- System/dependencies/query/dirtying: Content.Trauma.Shared/Heretic/Systems/SharedHereticCombatMarkSystem.cs. Its protected Timing and private _audio illustrate different field shapes, not a universal private-only rule.
- Component fields/network selection: Content.Trauma.Shared/Heretic/Components/HereticCombatMarkComponent.cs.
- Test entry: Content.IntegrationTests/Tests/Access/AccessReaderTest.cs, namespace Content.IntegrationTests.Tests.Access; inspect its current GameTest/TestPrototypes/sided dependencies when authoring a related test.
- Partial prototype examples: Resources/Prototypes/_Trauma/Partials/; validate current loader and Arcane destination before adapting them.

Read only the example relevant to the task. Do not recount type/dependency frequency or load every example before ordinary logic. Follow [verification](../../rules/verification.md) for checks.
