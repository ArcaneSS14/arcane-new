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

The repository builds with `LangVersion` `14` on `net10.0`. Use C# 14 features when they make the code or API clearer. Do not rewrite working code just to adopt new syntax; follow the owning project's patterns and keep behavior obvious at the call site. Detailed house style remains in `.agents/rules/csharp-writing-conventions.md`.

- `field` is a contextual keyword in property accessors. A field-backed property can remove a trivial backing-field pair, but keep an explicit field when it improves clarity or participates in component serialization, attributes, or other field-based APIs. Inside accessors, write `this.field` or `@field` when referring to an existing member with that name; avoid declaring a local or parameter named `field` there.
- C# 14 extension blocks add extension properties and operators, as well as static extension members. Use them when those member forms are needed or when grouping several related extensions improves discoverability. Keep a classic `this`-parameter extension method for a straightforward single method; do not convert existing extensions wholesale. Local examples are `Content.Server/Database/EFCoreExtensions.cs` and `Content.IntegrationTests/NUnit/Constraints/CompConstraintExtensions.cs`. Neither syntax grants access to private state.
- New implicit conversions make `Span<T>` and `ReadOnlySpan<T>` overloads applicable in more calls, which can change overload selection. Check the bound overload where behavior depends on it, especially in `Expression<Func<...>>` passed to EF Core: an interpreted expression cannot execute a span-based method. Call `Enumerable` explicitly or use an appropriate non-span value when the expression tree must stay provider-compatible. Relevant local expression-tree code is in `Content.Server/Database/EFCoreExtensions.cs` and `Content.Server/Database/ServerDbBase.cs`.
- Null-conditional assignment (`target?.Property = value`) is useful when the assignment should simply be skipped for a null target. Prefer an explicit branch if it needs logging, multiple effects, or makes the mutation harder to see.
- `nameof(List<>)` can name an unbound generic type. Use it when that is the intended symbol; do not add generic syntax just to make a name look more precise.
- Lambda parameter modifiers, partial constructors/events, and user-defined compound-assignment operators are available. Use them only when the modifier, generated partial implementation, or distinct compound operation is part of the design. They are uncommon here and should not be introduced only to demonstrate the language version.
- `extension` has contextual keyword uses in C# 14. Avoid introducing a type named `extension`; if an existing identifier collides in an extension declaration context, use the compiler-supported escaped identifier or rename only when compatibility allows.

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

**A partial prototype override.** `Resources/Prototypes/_Trauma/Partials/` shows the current Trauma-owned convention. For Arcane-owned changes, verify whether a corresponding `_Arcane/Partials` path is supported before using it. Partial prototypes can replace named fields, merge components, and use `!Remove` or `!Clear` where the schema permits.

**A test.** `Content.IntegrationTests/Tests/Access/AccessReaderTest.cs` is the shape to copy: a `sealed class XTest : GameTest` in `Content.IntegrationTests.Fixtures`, `[TestOf(typeof(Target))]` on the class, inline `[TestPrototypes]` YAML as a `const string` for fixtures the test needs, `[SidedDependency(Side.Server)]` for an injected system, and `[RunOnSide(Side.Server)]` on the method. `[TestFixture]` is used in 150 files in `Content.IntegrationTests` but is not required. Match the namespace style of the file you are editing; both block-scoped and file-scoped appear in this project.

**An update query.** Existing code orders `EntityQueryEnumerator` by the rarest component first; avoid starting with ubiquitous components such as `TransformComponent`. For example, `EntityQueryEnumerator<CrackedLanternSummonComponent, MeleeWeaponComponent, PhysicsComponent, ...>` puts the feature component first and `TransformComponent` last. Follow the current repository examples and the performance skill rather than relying on a named pattern that may not exist in this checkout.

## Collections and allocation

Choose collections from semantics. Avoid unnecessary LINQ in hot paths and do not return mutable internal collections where callers should not mutate state.

Prefer collection expressions for empty and inferred shapes, `[]` over `new List<T>()`. When a method takes a `Func`, provide an overload accepting caller-supplied state rather than forcing a closure capture. Full allocation rules: `.agents/rules/csharp-writing-conventions.md`.

## Comments and compatibility

Explain why, compatibility constraints, or non-obvious framework behavior. Do not narrate obvious code.

Comment inside components and prototype types, where a `[DataField]` member's documentation is the only thing a content contributor sees. In systems, comment only logic that would otherwise require reconstruction: ordering constraints, deliberate bounds, framework workarounds, derivations, and invariants. No section banners. Full rules: `.agents/rules/commenting-conventions.md`.

Treat public methods, events, serialized fields, prototype IDs, CVars, and network payloads as compatibility surfaces. Check all consumers before broad changes.
