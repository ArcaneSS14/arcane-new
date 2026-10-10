
How C# reads in this repository, and the modern form of that style under `LangVersion` 14 on `net10.0`.

These rules are derived from conventions the upstream project already enforces, from the shape of code already in this repository, and from contributor-authored code in this tree. They describe the house style, not personal preference. Where this repository and upstream disagree, match the surrounding file.

## File layout

- `using` directives first, then file-scoped namespace, then types.
- One primary type per file, named after the file.
- Fields and auto-properties before every method. Readers orient by looking at the data first.
- Non-auto properties (`=> _field.Trim()`) are not fields. Keep them with the methods, not above them.
- Mark the type `sealed`, `static`, `abstract`, or `[Virtual]`. A class with no such marker is an oversight. `sealed` is the default choice; `sealed partial class` appears roughly 3000 times in `Content.Shared` alone.

## Reuse over duplication

Do not copy and paste logic. If two places need the same behavior, extract a method or type that both call. The cost of duplication is paid later by whoever must find every copy, which is usually nobody.

Boilerplate is the exception. The skeleton of a new `EntitySystem`, a migration, or a test is intentionally repetitive because there is no meaningful abstraction to share.

## No magic values

A magic value is one that must equal something in another location. Strings, counts, and thresholds that are compared against YAML, a prototype, a network payload, or another method must not be literals at the comparison site.

- Prototype references become `static readonly ProtoId<T>` or `EntProtoId`, which validation tooling checks.
- Repeated constants become `const` or `static readonly`.
- Two values that must stay equal become one symbol, so a mismatch is a compile error rather than a runtime bug.

## Comments explain why

Comment the reasoning, not the mechanics. A comment restating the code is noise and ages badly.

```csharp
// Don't let players who drink cognizine be eligible for a ghost takeover
if (HasComp<MindContainerComponent>(uid))
    return;
```

Document with XML comments on public methods, `[DataField]` members, and prototype types. `Content.Shared/Polymorph/PolymorphPrototype.cs` carries `<summary>` on essentially every member because the types are consumed from YAML where the signature is the only documentation.

Comments belong in components and prototype types, and beside genuinely non-obvious logic. They do not belong on straightforward statements, and there are no section banners. Placement rules and the measured density in this repository: `.agents/rules/commenting-conventions.md`.

## Modernize toward current C#

The surrounding file may predate these. Apply them when writing or touching a line, without a separate style-only refactor.

The repository uses `LangVersion` 14 on `net10.0`. The complete guidance for C# 14 language behavior and overload-resolution hazards lives in `.agents/skills/csharp-style/SKILL.md`; use that source instead of duplicating compiler details in this rule. Language features do not change the ownership, serialization, compatibility, or readability requirements below.

### Use `var` where the type is apparent

`.editorconfig` sets `csharp_style_var_for_built_in_types`, `_when_type_is_apparent`, and `_elsewhere` to `true`. Prefer `var` unless the type is genuinely unclear or is being declared for the first time with a non-obvious target.

Note the tension: `csharp_style_predefined_type_for_locals_parameters_members = true` also applies. When both fire, follow the nearby file.

### Prefer interpolation over concatenation

```csharp
// Prefer
return $"Job{loadout}";

// Over the string concatenation in Content.Shared/Clothing/LoadoutSystem.cs:150
return "Job" + loadout;
```

### Prefer collection expressions for empty and inferred shapes

`[]` for an empty collection, `[a, b]` for a literal list. This repository already uses collection expressions in over 200 files. `new List<T>()` with no initializer adds nothing.

### Use pattern matching for null and type tests

```csharp
// Prefer
if (gear is null)
    return null;

if (ent is TransformComponent transform)
    var pos = transform.Coordinates;

// Over
if (gear == null)
if (ent.TryGetComponent<TransformComponent>(out var t))
```

This aligns with the guard-clause style below and removes the classic out-variable reuse bug.

### Flatten nesting with guard clauses

```csharp
// Prefer
if (a is null)
    return;

if (b is null)
    return;

DoWork();

// Over an if/else pyramid or a wrapper block containing a single if with a return
```

Do not nest more than two levels of control flow. Extract a method instead of adding a brace level.

### Use `field` for trivial accessors

```csharp
// Acceptable in new code when it removes a pure pass-through
private int _count;
public int Count
{
    get => field;
    set => field = value;
}
```

Prefer this only where the accessor is trivial. A setter with real logic still needs a backing field, and repository convention is still explicit backing fields, so match the file you are editing. Never introduce a member named `field`; see the diagnostics note in `.agents/skills/csharp-style/SKILL.md`.

### Use `extension` blocks only where they add something

A classic `this`-parameter extension method cannot declare operators, static properties, or instance properties. When you need one of those, use an `extension` block. Otherwise use the classic form. This repository has both, including `Content.Server/Database/EFCoreExtensions.cs` line 9 and `Content.IntegrationTests/NUnit/Constraints/CompConstraintExtensions.cs` line 21. Neither form can reach private state of the extended type.

### Name `TimeSpan` durations, not raw seconds

Durations in component fields use `TimeSpan`. Comparison in update loops goes against `CurTime`, not accumulated frame time. Runtime-modified timers need `[AutoGenerateComponentPause]` on the component and `[AutoPausedField]` on each field; absolute times use `TimeOffsetSerializer`.

### Use `System.Threading.Lock` for new synchronization

`Lock` is the dedicated type in .NET 10 and avoids the `lock (bool)` and `lock (this)` hazards. No existing code in this repository uses it, so do not convert working locking as a side effect of an unrelated change.

## Allocation discipline

- Return iterators over materializing a collection, unless a collection is genuinely needed by the caller. Iterators allocate per enumeration, so avoid `yield` in per-frame paths where a reusable buffer works.
- Avoid lambda variable capture in hot paths. When a method takes a `Func`, add an overload that accepts caller-supplied state instead of forcing a closure:

```csharp
// Prefer
MethodWithPredicate<EntityUid>(predicate, otherEntity);

// Over
var predicate = (EntityUid uid) => uid == otherEntity;
MethodWithPredicate(predicate);
```

- Avoid LINQ in per-tick and per-entity update loops. Do not convert working, readable loops as a side effect of an unrelated change.
- Do not return a mutable internal collection. Return a read-only view, or copy when the caller needs its own.

## Strings and localization

- Never use human-readable text as an identifier, and never use an identifier as user-facing text. No localized string in a dictionary key, no `==` against one, no raw `Enum.ToString()` handed to a control.
- Player-facing strings come from `Loc.GetString` with a typed key. Keys are kebab-case, feature-scoped, and specific enough not to collide.

```csharp
// Prefer
GenderLabel.Text = Loc.GetString($"gender-{gender}");

// Over
GenderLabel.Text = gender.ToString();
```

- Use `CurrentCulture` comparisons for user-visible search and filter text, not invariant comparison.

## Property setters

A property setter must assign the given `value` literally. Validation or clamping belongs in a system method, with `[Friend]` on the component restricting mutation to that owner.

```csharp
// Forbidden
public string Name
{
    get => _name;
    private set => _name = Loc.GetString(value);
}
```

Setter logic is permitted only for ViewVariables integration.

## Compatibility surfaces

Treat these as API. Before renaming or reshaping any of them, find every consumer, including YAML, migrations, map files, and network payloads:

serialized field names, prototype IDs, component and system type names, event names, network message fields, database columns, CVars, and locale keys.

Renum in the repository rather than to upstream. A same-named conflicting type is a different problem; do not resolve it by renaming.
