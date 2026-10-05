---
name: csharp-style
description: Write repository-consistent C# only after proving symbol access, ownership, and compatibility.
---

Style never overrides correctness, accessibility, or assembly boundaries.

## Before writing a call

Find the declaration of every non-local symbol and verify its signature, modifier, namespace, project, assembly, caller context, and project reference.

Do not infer an API from autocomplete-like names, old forks, search snippets, prompt examples, or neighboring code.

## Accessibility invariants

- `private` is available only inside the declaring type.
- `internal` is available only inside the declaring assembly unless explicit friendship applies.
- `protected` requires a valid derived-type access context.
- a project reference does not bypass modifiers.
- an extension method cannot access private state.
- partial declarations cannot combine across assemblies.
- a module DLL cannot add methods or fields to a core partial type.
- a sealed type cannot be inherited.
- runtime module loading does not grant compile-time type access.

When required access is unavailable, stop and propose a real public or internal extension point in the correct owner. Do not use reflection, copied private logic, or visibility widening solely to make an implementation compile unless explicitly requested.

## Structure

Keep files focused and names discoverable. Prefer explicit domain names over generic `Manager`, `Data`, or `Helper`.

Keep public APIs small but sufficient for real callers. Do not expose mutable internals. If cross-assembly behavior is intended, design an explicit stable API rather than relying on implementation details.

Fields and auto-properties come before every method, so a reader can orient on the data first. Mark every type `sealed`, `static`, `abstract`, or `[Virtual]`. Full layout, reuse, and comment rules: `.agents/rules/csharp-writing-conventions.md`. ECS and prototype layout: `.agents/rules/ecs-writing-conventions.md` and `.agents/rules/yaml-prototype-conventions.md`.

For the frequently injected dependency types, their canonical field names, and the most common `using` namespaces, see `references/dependency-injection.md`. Use it to match a neighbour's naming; still verify the declaration before calling it.

## Language version

The repository builds with `LangVersion` `14` on `net10.0`. C# 14 features are available, but match nearby style before introducing them.

- `field`-backed properties are available. Prefer the existing explicit backing field pattern already used across this repository, and adopt `field` only in new code where it removes a trivial getter/setter pair.
- `field` is a contextual keyword inside property accessors. This repository already uses `field` as a local variable or parameter name in 43 files. Two distinct diagnostics matter, and they are not the same:
  - declaring a local or parameter named `field` inside an accessor is error **CS9272**, not a warning;
  - referencing an existing member named `field` from an accessor is warning **CS9258**, because `field` now binds to the synthesized backing field instead.
  Use `this.field` or `@field` to disambiguate. Do not introduce a member named `field` on a type that has properties.
- `extension` blocks are already used in this repository, for example in `Content.Server/Database/EFCoreExtensions.cs` (line 9, `extension<TEntity>(IQueryable<TEntity> query)`) and `Content.IntegrationTests/NUnit/Constraints/CompConstraintExtensions.cs` (line 21, `extension(Has)`). Match the surrounding file: use an `extension` block for operators, static properties, or instance properties that a classic extension method cannot express, and a classic `this`-parameter method otherwise. Neither form can reach private state of the extended type.
- Overload resolution changed for `Span` and `ReadOnlySpan`, and `ReadOnlySpan` is now generally preferred over `Span`. Two consequences worth checking in new code:
  - Inside an `Expression<Func<...>>` lambda, a span-based overload such as `MemoryExtensions.Contains` may now bind where `Enumerable.Contains` used to. Interpreted expressions then throw at runtime. Bind explicitly with a cast to the exact parameter type, `AsEnumerable()`, or a static `Enumerable.Contains(...)` call. `Content.Server/Database/EFCoreExtensions.cs` and `Content.Server/Database/ServerDbBase.cs` build expression trees passed to EF Core, so prefer the non-span overload there.
  - The `array.Reverse()` break from C# 14 does not apply here: .NET 10 added `Enumerable.Reverse<T>(T[])`, which wins overload resolution over the in-place `MemoryExtensions.Reverse`. Only downlevel targets are affected, and this repository targets `net10.0` exclusively.
- A type, alias, or type parameter named `extension` is disallowed in C# 14. Use `@extension` if a name collision is unavoidable.

## Nullability and entities

Respect nullable annotations and established entity/component patterns. Avoid null-forgiving operators unless an invariant is proven immediately nearby.

`Nullable` is `enable` by default through `MSBuild/Content.props`. Several test and tooling projects disable it deliberately. Verify the project before assuming nullable reference analysis is active.

## Control flow

Prefer guard clauses. Keep event handlers thin. Avoid duplicated validation and deeply nested logic. Early returns must not bypass cleanup.

Prefer pattern matching over null and type tests: `if (gear is null)` over `if (gear == null)`, `if (ent is TransformComponent transform)` over a `TryGetComponent` with an out variable. Keep nesting to two levels by extracting a method instead of adding a brace.

## Worked examples

Read these before writing non-trivial logic. Each is a complete, small file in the tree.

**A system, end to end.** `Content.Trauma.Shared/Heretic/Systems/SharedHereticCombatMarkSystem.cs`, 59 lines, `abstract partial` shared base with no subscriptions because it is called directly.

The dependency block encodes the naming rule by itself: `[Dependency] protected IGameTiming Timing` has no underscore because it is `protected`; `[Dependency] private SharedAudioSystem _audio` has one. A cached `EntityQuery<T>` field and a `readonly HashSet` reused via `Clear()` replace per-call allocation. The handler opens with an early return before allocating, guards a string-built prototype ID with `ProtoMan.HasIndex<EntityEffectPrototype>`, calls `Dirty` right after mutation, uses `RemCompDeferred` instead of `RemComp`, and carries two trailing comments that justify ranking choices rather than restate code.

**A component.** `Content.Trauma.Shared/Heretic/Components/HereticCombatMarkComponent.cs`. Every field is `[DataField]` with a meaningful initializer, only the field clients need is `[AutoNetworkedField]`, `[AutoPausedField]` sits on the field a timer compares against, and a `customTypeSerializer` is declared where the default would be wrong. The companion enum lives in the same file.

**A partial prototype override.** `Resources/Prototypes/_Trauma/Partials/`, the required form per `CONTRIBUTING.md`. The pattern from `CONTRIBUTING.md` shows the four operations that matter: naming a field replaces it, naming a component merges into the existing one, `!Remove` deletes a quality, and `!Clear` replaces a whole map.

**A test.** `Content.IntegrationTests/Tests/Access/AccessReaderTest.cs` is the shape to copy: a `sealed class XTest : GameTest` in `Content.IntegrationTests.Fixtures`, `[TestOf(typeof(Target))]` on the class, inline `[TestPrototypes]` YAML as a `const string` for fixtures the test needs, `[SidedDependency(Side.Server)]` for an injected system, and `[RunOnSide(Side.Server)]` on the method. `[TestFixture]` is used in 150 files in `Content.IntegrationTests` but is not required. Match the namespace style of the file you are editing; both block-scoped and file-scoped appear in this project.

**An update query.** `CONTRIBUTING.md` is explicit: order an `EntityQueryEnumerator` by the rarest component first, and never write `EntityQueryEnumerator<TransformComponent, MyComponent>`, which walks every entity. The ordering rule does hold in the tree: `EntityQueryEnumerator<CrackedLanternSummonComponent, MeleeWeaponComponent, PhysicsComponent, ...>` puts the feature component first and `TransformComponent` last. The `ActiveXComponent` pattern that `CONTRIBUTING.md` names as the example has zero occurrences in the current codebase, so copy the ordering principle rather than that type.

## Collections and allocation

Choose collections from semantics. Avoid unnecessary LINQ in hot paths and do not return mutable internal collections where callers should not mutate state.

Prefer collection expressions for empty and inferred shapes, `[]` over `new List<T>()`. When a method takes a `Func`, provide an overload accepting caller-supplied state rather than forcing a closure capture. Full allocation rules: `.agents/rules/csharp-writing-conventions.md`.

## Comments and compatibility

Explain why, compatibility constraints, or non-obvious framework behavior. Do not narrate obvious code.

Comment inside components and prototype types, where a `[DataField]` member's documentation is the only thing a content contributor sees. In systems, comment only logic that would otherwise require reconstruction: ordering constraints, deliberate bounds, framework workarounds, derivations, and invariants. No section banners. Full rules: `.agents/rules/commenting-conventions.md`.

Treat public methods, events, serialized fields, prototype IDs, CVars, and network payloads as compatibility surfaces. Check all consumers before broad changes.
