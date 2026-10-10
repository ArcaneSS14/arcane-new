
A port is easy to paste into an existing upstream file. Choose its owner before implementation and place Arcane-only behavior in Arcane-owned projects and resources.

## Port and cherry-pick destination by provenance

The default destination for every port or cherry-pick is the corresponding Arcane-owned project or resource path. This keeps imported work in paths owned exclusively by Arcane and reduces conflicts with other forks and upstream files. “Arcane module” means the root-level `Content.Arcane.*` projects and existing `_Arcane` resource directories; `Modules/Arcane` contains guidance, not runtime code. Port behavior into the correct Arcane layer; do not copy files mechanically when the destination architecture differs.

Exception: when source history confirms that the change is an addition made by TraumaStation, keep it on the corresponding Trauma-owned or vanilla path so the TraumaStation sync can carry it forward. Do not relocate that upstream addition into Arcane solely because it arrived through a port or cherry-pick. Apply Arcane edit markers to any Arcane-authored adaptation on that inherited path; preserve upstream authorship markers on unchanged source lines.

Revert/restoration exception: when the requested change restores content removed by a TraumaStation revert or other Trauma-side deletion, put the restored file and behavior in Arcane-owned paths, even if the original file belonged to TraumaStation or the restoration is delivered as a new file. Do not reintroduce the removed content into the Trauma-owned file or treat the revert as a new TraumaStation addition eligible for the sync exception. This keeps the restoration isolated from the upstream deletion and avoids recurring conflicts. Trace the original addition, removal, and any follow-ups under `.agents/rules/change-history-analysis.md`; port the intended behavior adapted to current APIs, not a blind copy of the reverted patch. If a small integration change in a Trauma/base file is unavoidable, keep it minimal and mark it as Arcane.

Determine provenance from the actual source commit and diff, not only a PR title, branch name, marker, or the fact that the file currently lives under `Content.Trauma.*`. A TraumaStation marker alone does not prove that a change was added by TraumaStation. A restoration of Trauma-removed content follows the revert/restoration exception above and takes precedence over the ordinary TraumaStation-addition exception. If history is unavailable or does not establish TraumaStation authorship, use the Arcane-owned destination by default and report the uncertainty. Place Arcane-specific adaptations in Arcane-owned paths even when they derive from TraumaStation behavior.

Follow `.agents/rules/change-history-analysis.md` when a port, revert, cherry-pick, or restoration depends on history. This rule sets Arcane ownership as the default to minimize conflicts and defines the narrow TraumaStation sync exception; keep any necessary inherited edits minimal and correctly marked.

## Where a port lands

| Port kind | Destination |
|---|---|
| gameplay system, component, event | root-level `Content.Arcane.Shared/`, `Content.Arcane.Server/`, or `Content.Arcane.Common/` |
| client UI | root-level `Content.Arcane.Client/` |
| prototype, map, texture | the existing Arcane-owned resource path, commonly `Resources/Prototypes/_Arcane/` or `Resources/Textures/_Arcane/` |
| localization | existing `Resources/Locale/en-US/_Arcane/` path; update `ru-RU/_Arcane/` only when explicitly requested |
| a change to an existing base type that the port cannot avoid | the base file, smallest possible diff, Arcane markers, reported |

Read root and nearest scoped `AGENTS.md` files for the chosen path. Verify the exact resource path and project references before writing. `Modules/Arcane` currently contains guidance files and is not a runtime destination.

## Why Arcane

`arcane-new` is a fork of TraumaStation. TraumaStation is the sync source. A port pasted into a vanilla root path or a Trauma-owned path becomes a recurring reconciliation cost on every import, and a port is large enough that the cost is real.

A port in `Content.Arcane.*` or an Arcane-owned resource directory is owner-local. It has no marker requirement and no upstream to collide with.

Ports also carry behavior that does not exist upstream. That is exactly what an owner-local module is for: it can differ without arguing with anyone.

## When the destination must be shared

Some ports cannot live in one project. Keep the port's own layers separate and do not widen references to work around it:

- a contract both client and server need goes in `Content.Arcane.Shared`
- server authority and hidden state go in `Content.Arcane.Server`
- presentation and EUI go in `Content.Arcane.Client`
- a type needed below gameplay Shared goes in `Content.Arcane.Common`

`CONTRIBUTING.md` sets the dependency direction. Common may not depend on Arcane Shared, Server, or Client. Do not add a reference in order to make a port compile; change the layer instead.

## What not to port

Before excluding or restoring behavior that appears in a revert, deletion, or follow-up commit, follow `.agents/rules/change-history-analysis.md` and verify the final source state.

Do not port a feature that already exists here in equivalent form. Search first:

```powershell
git grep -n "FeatureName" -- '*.cs' '*.yml'
git grep -n "feature-locale-key" -- Resources/Locale
```

Port final intended behavior, not historical broken states. When the source had a bug that was later fixed, port the fix and say so in the delivery note. When the source relies on an API that has since changed, adapt to the current API rather than reproducing the old call.

Do not port another fork's ownership into the tree. A source marker from `Trauma - ` or `Goobstation-` does not travel; the port gets Arcane markers.

## Localization

`en-US` is the structural source of truth and is the required locale by default. Do not add or synchronize Russian as an automatic part of a port. If Russian is explicitly requested, preserve matching keys, attributes, variables, selectors, and ordering.

## Assets and third-party material

Sprites and audio use the existing resource roots and Arcane owner directories where present. Verify attribution before adding anything with a license attached, and check whether an equivalent RSI already exists in `_Arcane` before creating a new one.

For an upstream asset resprite, follow the established owner and attribution convention for that asset. A port of a third-party asset is not an upstream resprite; place it in the appropriate Arcane-owned resource directory and resolve licensing before it lands.

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
dotnet build Content.Arcane.Shared/Content.Arcane.Shared.csproj --no-restore
git diff --check
git status --short
```

Build the smallest affected project first. For a port that touched a base file, also build that base project. Run the existing owner test project if one covers the destination; do not create new test infrastructure for a port.

Confirm no marker from the source fork survived, and no ported code landed in a vanilla root path.
