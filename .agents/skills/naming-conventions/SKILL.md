---
name: naming-conventions
description: Name C# symbols, events, prototypes, FTL keys, resources, and compatibility surfaces consistently.
---

Names must communicate ownership, timing, and intent.

## C#

Use repository-standard casing and suffixes. Components and systems should be discoverable by type search. Events should state whether they are attempts, pre-change checks, notifications, or completed changes.

Do not introduce an abbreviation that is not established project vocabulary.

A partial class added by a fork should follow the suffix already used for that owner and file family, placed after the semantic suffix: `MobStateSystem.Trauma.cs`, `ActionsSystem.Goob.cs`, `EnergySwordSystem.Arcane.cs`. Before adding a partial, verify that the type can be extended in the same assembly and read the applicable owner and sync rules. Never rename an existing partial to a different fork's suffix or name a new partial after a fork that does not own the code.

## Fork-suffixed partials as a conflict tool

`Content.Shared`, `Content.Client`, `Content.Server`, and `Content.Common` are the conflict surface, because sync brings upstream edits into the same files. A suffixed partial is how a fork adds behavior without editing a base file.

Before editing a base declaration, check whether a partial can carry the change. An owner-suffixed partial can keep additive behavior out of a shared upstream file when the type and assembly support it. Details: `.agents/rules/fork-trajectory-priority.md`.

## Prototypes and resources

Prototype IDs are stable machine identifiers, not display names. Use a feature prefix where collisions are possible. Keep directory names, RSI states, audio collections, map names, and nearby prototypes aligned.

## Localization

FTL keys use lowercase kebab-case and a stable owner or feature prefix. Variable names describe meaning, not UI position.

Use stable keys and variables in `en-US`. Keep them consistent with `ru-RU` only when Russian localization is explicitly in scope. A module key prefix must make ownership clear. File placement does not protect duplicate keys.

## Compatibility

Before renaming a serialized field, prototype ID, map entity, database field, CVar, locale key, or network message, search code, prototypes, maps, migrations, config, the affected locale, and downstream references; include Russian files only when Russian is in scope. Provide a migration or compatibility alias where required.

## Verification commands

```powershell
git grep -n "OLD_NAME" -- path/to/relevant/code path/to/relevant/resources path/to/relevant/config
git grep -n "NEW_NAME" -- path/to/relevant/code path/to/relevant/resources path/to/relevant/config
dotnet build path/to/affected-project.csproj --no-restore
git diff --check
```

Search all references only when the renamed identifier is a public compatibility surface or the task requires finding every reference. For prototype or locale changes, use a targeted validator when available; follow root `AGENTS.md` for verification scope instead of automatically running a Release build or global YAML linter.
