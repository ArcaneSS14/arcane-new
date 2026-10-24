
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


Names describe intent and timing: TryPurchase validates and executes; CanPurchase does not mutate; Before/Attempt/Completed events must state whether cancellation/handled results are meaningful. Use established domain vocabulary rather than new abbreviations or vague Handle/Process/Data names.

Match case exactly for resource directories/states, including Linux consumers. Keep released paths, sprite states and audio collection identities stable; collection names express selection purpose, not an individual file name. FTL keys/variables describe feature and meaning, e.g. bank-transfer-error-insufficient-funds rather than error-2; file placement does not prevent duplicate keys.

For a renamed compatibility surface, trace actual code/data/config/downstream consumers and stale variants, with migration/alias where needed. Search all references when the public surface requires it; a local variable rename does not justify every resource/build check. Locale scope remains in [content policy](content-and-localization.md), partial placement in [trajectory](fork-trajectory-priority.md).
