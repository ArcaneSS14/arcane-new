
A port is the one task where it is most tempting to paste into whatever file already has the behavior. Do not.

Ported changes go into Arcane-owned paths. That is the default, not a preference.

## Where a port lands

| Port kind | Destination |
|---|---|
| gameplay system, component, event | `Modules/Arcane/Content.Arcane.Shared/`, `Content.Arcane.Server/`, or `Content.Arcane.Common/` |
| client UI | `Modules/Arcane/Content.Arcane.Client/` |
| prototype, map, sprite, audio, FTL | `Modules/Arcane/Resources/` |
| localization | `Modules/Arcane/Resources/Locale/en-US/` and `ru-RU/` |
| a change to an existing base type that the port cannot avoid | the base file, smallest possible diff, Arcane markers, reported |

Read `Modules/Arcane/AGENTS.md` before writing into it. `Modules/Lavaland/` is also Arcane-owned and acceptable when the port belongs to that module's declared scope.

## Why Arcane

`arcane-new` is a fork of TraumaStation. TraumaStation is the sync source. A port pasted into a vanilla root path or a Trauma-owned path becomes a recurring reconciliation cost on every import, and a port is large enough that the cost is real.

A port in `Modules/Arcane` is owner-local. It has no marker requirement and no upstream to collide with.

Ports also carry behavior that does not exist upstream. That is exactly what an owner-local module is for: it can differ without arguing with anyone.

## When the destination must be shared

Some ports cannot live in one project. Keep the port's own layers separate and do not widen references to work around it:

- a contract both client and server need goes in `Content.Arcane.Shared`
- server authority and hidden state go in `Content.Arcane.Server`
- presentation and EUI go in `Content.Arcane.Client`
- a type needed below gameplay Shared goes in `Content.Arcane.Common`

`Modules/Arcane/AGENTS.md` sets the dependency direction. Common may not depend on Arcane Shared, Server, or Client. Do not add a reference in order to make a port compile; change the layer instead.

## What not to port

Do not port a feature that already exists here in equivalent form. Search first:

```powershell
git grep -n "FeatureName" -- '*.cs' '*.yml'
git grep -rn "feature-locale-key" -- Resources/Locale Modules/Arcane/Resources/Locale
```

Port final intended behavior, not historical broken states. When the source had a bug that was later fixed, port the fix and say so in the delivery note. When the source relies on an API that has since changed, adapt to the current API rather than reproducing the old call.

Do not port another fork's ownership into the tree. A source marker from `Trauma - ` or `Goobstation-` does not travel; the port gets Arcane markers.

## Localization

`en-US` is the structural source of truth. Every English key added by a port needs a `ru-RU` counterpart, in the same position, with the same attributes, variables, and selectors. Do not ship an English-only port key and report it as follow-up.

## Assets and third-party material

Sprites and audio arrive under `Modules/Arcane/Resources/`. Verify attribution before adding anything with a license attached, and check whether an equivalent RSI already exists in `_Arcane` before creating a new one.

`CONTRIBUTING.md` says a resprite of an upstream asset amends the copyright line in place rather than duplicating the RSI into `_Trauma`. That instruction is for upstream assets. A port of a third-party asset is not a resprite of an upstream asset, so it gets its own file under `Modules/Arcane/Resources/`, and the license question must be answered before it lands.

## Records

The delivery note for a port states:

- source repository, source commit, root PR, follow-up PRs
- what was excluded and why
- licensing evidence for every imported asset
- the destination path list
- the count of base-path files edited, which should be as close to zero as the port allows
- checks that were not run

## Verification

```powershell
dotnet build Modules/Arcane/Content.Arcane.Shared/Content.Arcane.Shared.csproj --no-restore
git diff --check
git status --short
```

Build the smallest affected project first. For a port that touched a base file, also build that base project. Run the existing owner test project if one covers the destination; do not create new test infrastructure for a port.

Confirm no marker from the source fork survived, and no ported code landed in a vanilla root path.
