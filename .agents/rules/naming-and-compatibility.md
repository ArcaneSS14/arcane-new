
Follow established repository vocabulary before inventing a new term.

- C# types use PascalCase, members follow nearby style, and private fields use the repository convention.
- Components end with `Component`; systems end with `System` unless a framework base defines another established pattern.
- Events describe timing and direction clearly, such as `Before`, `Attempt`, `Changed`, or `Completed`.
- Prototype IDs are stable API-like identifiers. Do not rename them casually.
- FTL keys use lowercase kebab-case with a feature-scoped prefix.
- Resource paths and sprite states use stable lowercase naming consistent with nearby assets.

## Shared type prefixes

A type is prefixed `Shared`, `Client`, or `Server` if and only if a same-named counterpart exists in another assembly.

- `FooComponent` in Shared only: no prefix.
- `FooComponent` in Shared, Client, and Server: Shared keeps `SharedFooComponent`, the others take `ClientFooComponent` and `ServerFooComponent`.

## Entity and event naming

Entity handlers are `OnXEvent`. Events are `record struct` with `[ByRefEvent]` and a verb phrase: `AnchorAttemptEvent`, `DamageChangedEvent`, `BeforePolymorphedEvent`.

Methods that act on entities take `Entity<T?>` or `EntityUid` first. Methods that ask permission are `CanX` and do not mutate. Methods that validate fully are `TryX` and return success.

Systems are `FooSystem`. Managers, where they exist, are `FooManager` only when they are genuinely lifecycle-owning singletons. Avoid a new `Manager` for something an `EntitySystem` covers.

Treat serialized field names, prototype IDs, network contracts, database columns, map entities, and saved configuration as compatibility surfaces. Renames require migration, aliases, or an explicit compatibility decision.
