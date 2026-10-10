
How entity systems, components, events, and prototypes are written here, and the modern form of that style under `LangVersion` 14.

For symbol access and assembly boundaries, see `architecture-and-ownership.md` and `coding-and-api-design.md`. This file covers how ECS code reads.

## System structure

```csharp
namespace Content.Shared.Foo;

public sealed partial class FooSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FooComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);
    }
}
```

- `namespace` is `Content.Shared/Server/Client.<Domain>`, never shortened.
- `sealed partial`, one system per concern.
- Dependencies are `[Dependency] private` fields, not `readonly` in current code. Do not call `IoCManager.Resolve<T>()` inside a system; it hides the dependency and defeats ordering. This is the dominant pattern: `IGameTiming`, `SharedPopupSystem`, and `SharedAudioSystem` appear as dependencies in 709, 483, and 446 declarations.
- Match the field name to what the surrounding files use. `_timing`, `_popup`, `_audio`, and `_transform` are the established names. Full table of frequent dependencies, field-name variants, and common `using` namespaces: `.agents/skills/csharp-style/references/dependency-injection.md`.
- `SubscribeLocalEvent` in `Initialize` only. Order matters when two handlers must see consistent state, so use `after:` and `before:` rather than relying on registration accident.
- `Initialize` calls `base.Initialize()` first.
- Handlers are `private void On<Thing><Event>`.
- Systems hold no state. Per-entity state belongs in a component.

## Public method signature

All `EntityUid` and `Entity<T?>` parameters come first, then gameplay arguments. The first statement in the body resolves:

```csharp
/// <summary>
/// Sets the stack count.
/// </summary>
public void SetCount(Entity<StackComponent?> stack, int count)
{
    if (!Resolve(stack, ref stack.Comp))
        return;

    stack.Comp.Count = count;
}
```

Over 600 call sites in this repository already use `if (!Resolve(...))`. Use the `?` on the component type to mark it optional, and pass `logMissing: false` when a missing component is an expected outcome rather than an error.

`Resolve` also verifies in `DEBUG` that a non-null component actually belongs to the entity. Do not replace it with a manual `TryGetComponent`.

Reach for `Resolve` overloads for 2 to 4 components. For more components on one entity, make several calls.

## Method Events versus system methods

A system method is the public API. An event is the mechanism underneath it, and only when decoupling is genuinely needed.

```csharp
// Public API on the owning system
public void ChangeDamage(EntityUid uid, DamageSpecifier kind, FixedPoint2 amount)
{
    var ev = new DamageChangeEvent(kind, amount);
    RaiseLocalEvent(ref ev, uid);
}
```

Do not raise a new event merely to reach into another system's component.

## Events

- Events are `record struct`, not class. This repository has over 600 `record struct` events.
- Mark every event `[ByRefEvent]` and raise it with `ref`:
  ```csharp
  var ev = new AnchorAttemptEvent(target, user);
  RaiseLocalEvent(ref ev, target);
  ```
- Use `readonly record struct` when the event carries no intent to mutate. 155 events in this repository do.
- Handlers take `ref <Event> args` for mutable events and plain `args` for readonly ones.
- Name the event for what happened, with a timing or direction prefix when useful: `BeforeAnchorAttemptEvent`, `DamageChangedEvent`, `AnchorAttemptFailedEvent`.
- `[ByRefEvent]` on a network event additionally needs `[NetSerializable]`.

Modernization: `readonly record struct` where nothing mutates. `record struct` over a hand-written struct gets `Equals`, `GetHashCode`, and pattern matching for free.

## Components

Components hold data and no behavior.

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StackComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Count;
}
```

- `DataField` members use fields, not properties, unless the prototype requires a computed shape.
- Add `required: true` only when the field has no sensible default.
- Do not add setter logic. Use a system method and `[Friend(...)]` to restrict mutation to that owner.
- Use `[Access]` to restrict read or write to a named type when the component is shared and the boundary is worth enforcing.
- Use `fieldDeltas: true` on `AutoGenerateComponentState` when fields change independently at different rates. With three or more independently-changing fields it can cut traffic substantially. Call `DirtyField(uid, comp, nameof(...))` instead of `Dirty` so only that field is sent.

Existing code uses the `[DataField] public List<ProtoId<T>> Field = new();` shape. Keep it; `[DataField] public List<ProtoId<T>> Field { get; } = default!;` is equivalent here.

A fuller real component, `Content.Trauma.Shared/Heretic/Components/HereticCombatMarkComponent.cs`:

```csharp
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class HereticCombatMarkComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public HereticPath Path = HereticPath.Blade;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDisappear;

    [DataField]
    public SoundSpecifier? TriggerSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/repulse.ogg");
}
```

- every field is `[DataField]`, so it is settable from YAML and documented by its own initializer
- only `Path` is `[AutoNetworkedField]`, because the rest is server-only runtime state
- `[AutoPausedField]` on the field a timer compares against, so it does not drift while paused
- an explicit `customTypeSerializer` where the default would be wrong
- the enum driving the sprite `Key` is declared in the same file, next to the component

## Worked example

`Content.Trauma.Shared/Heretic/Systems/SharedHereticCombatMarkSystem.cs` shows most of this rule in one 59-line file.

```csharp
[Dependency] protected IGameTiming Timing = default!;      // protected, so it has no underscore
[Dependency] private SharedAudioSystem _audio = default!; // private, so it is underscore-named
[Dependency] private EntityQuery<MobStateComponent> _mobQuery = new();

private readonly HashSet<Entity<HumanoidProfileComponent>> _lookupHumanoid = new();
```

The dependency naming rule is visible in the field declarations themselves: a `protected` dependency is named without an underscore, a `private` one with it. A cached `EntityQuery<T>` field replaces per-call `new()`, and a `readonly HashSet` reused via `Clear()` replaces a fresh collection per call.

`ApplyMarkEffect` shows the shape most effect handlers should have:

```csharp
public void ApplyMarkEffect(EntityUid target, HereticCombatMarkComponent mark, EntityUid user)
```

- an early return as soon as the work is done, before any allocation
- `Dirty` immediately after mutating fields
- `RemCompDeferred` rather than `RemComp`, so removal lands at a safe point
- `ProtoMan.HasIndex<EntityEffectPrototype>(...)` guarding a string-built prototype ID before use
- two trailing comments that each justify a ranking decision, `// Prioritize living mobs` and `// Prioritize mobs nearby`. Neither restates the code it sits next to

Existing code orders `EntityQueryEnumerator` by the rare feature component first and the ubiquitous `TransformComponent` last. Follow that repository pattern and the performance skill; do not depend on an example type that is absent from this checkout.

The same file declares a compact event next to its raiser instead of in a shared events file:

```csharp
[ByRefEvent]
public readonly record struct UpdateCombatMarkAppearanceEvent;
```

## Prototype ID fields

Never cache a resolved prototype. Resolve it when needed and store the ID:

```csharp
[DataField]
public List<ProtoId<DamageDefinitionPrototype>> Types = new();
```

`ProtoId<T>` and `EntProtoId` are validated at load, so a typo fails fast. Bare strings for prototype references are not.

## Inheriting prototypes

`IInheritingPrototype` plus `[ParentDataField]`, `[NeverPushInheritance]`, and `[AbstractDataField]` is the established shape, as in `Content.Shared/Polymorph/PolymorphPrototype.cs`. Composable prototype hierarchies read better than a wide flat ID list.

Prefer prototypes over enums for anything data-driven or content-extensible. Enums force a code change to add a variant; a prototype does not.

## Extension methods

Do not add extension methods on `EntityUid`, components, or systems. The existing `EntityUid` extensions are legacy. Behavior belongs on the owning system so the call site is discoverable.

## C# language features in ECS code

Use the C# 14 guidance in `.agents/skills/csharp-style/SKILL.md` for language-specific behavior, including `field`-backed properties, extension blocks, and span overload resolution. In ECS code, keep `[DataField]` and other attribute-driven members in the shape required by their serializers and framework APIs; newer property syntax does not make a field-based contract interchangeable with a property. Do not modernize unrelated ECS code as a side effect.

## Anchoring

Use `TransformComponent` anchoring through system helpers. `PhysicsComponent` static-body anchoring is acceptable only when you can defend the specific reason, in the PR description.
