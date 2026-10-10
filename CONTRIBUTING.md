# Arcane contribution guidelines

This repository is an Arcane fork built on Space Station 14 and TraumaStation. These guidelines describe where Arcane work belongs and how to contribute safely. For project-specific instructions, read the root [AGENTS.md](AGENTS.md), the nearest scoped `AGENTS.md`, and the matching workflow in `.agents/SCENARIOS.md`. `.agents/CATALOG.md` routes task-specific skills; do not load every skill for a routine change.

## Contribution basics

- Keep changes focused and explain the player-visible or maintenance reason for them.
- For behavior fixes, add or extend a focused test that captures the expected result when the owning test project can cover it.
- Before opening or updating a pull request, review the diff, run checks that cover the changed behavior, and report exact commands and results.
- Include screenshots or a short video for meaningful visual or UI changes when they help reviewers.
- Address review feedback, then rerun the relevant checks before requesting review again.
- Add a changelog only for changes players or server operators should notice. See the format below.

## Where changes belong

Arcane runtime code is organized into root-level projects:

- `Content.Arcane.Common`: low-level types needed below gameplay Shared.
- `Content.Arcane.Shared`: shared components, systems, network contracts, and prediction-compatible behavior.
- `Content.Arcane.Server`: authoritative outcomes, server-only simulation, validation, and persistence coordination.
- `Content.Arcane.Client`: presentation, client-only systems, controls, and UI.

Follow the existing project references and dependency direction. A project name or matching namespace does not grant access across assemblies. Verify the declaration, accessibility, and project reference before using a type. Do not add references merely to make a misplaced type compile.

Arcane prototypes, textures, and locale files use the existing `_Arcane` directories under `Resources`. Preserve the established resource tree and exact path casing. Check for an existing owner file before creating another. Other resource types should follow the corresponding existing owner directory and nearby conventions.

Use `Content.Tests` or `Content.IntegrationTests` when their existing test ownership fits the behavior. Do not create a parallel Arcane test project or duplicate fixtures and CI setup without a demonstrated need.

## Choosing an owner and editing inherited code

Put Arcane-only behavior in `Content.Arcane.*` and Arcane-owned resource directories. Do not put feature behavior into a different fork's module simply because it has a similar project name.

Some changes may belong on the TraumaStation sync trajectory or in shared upstream code. Prefer an existing supported extension point and keep inherited-file changes small. Before changing an inherited path, check whether the behavior can live in an Arcane-owned project or resource directory. Follow `.agents/rules/fork-trajectory-priority.md` and `.agents/rules/arcane-edit-markers.md` for placement and markers; follow `.agents/rules/merge-conflict-resolution.md` only when resolving a real conflict.

Arcane-authored changes to inherited files use Arcane markers in the file's supported comment syntax. Do not put markers inside Arcane-owned projects or `_Arcane` resource directories. Preserve upstream markers on untouched lines; replace one only when changing that line. If the file format cannot safely contain the required marker, do not break the file to force one in; report the limitation and follow the focused marker guidance.

## C# and gameplay behavior

Choose the narrowest project that owns the behavior. Keep client presentation out of Server, authority and hidden state out of Client, and Shared logic safe for both client and server execution. Validate client-originated requests on the server. Keep components focused on state and put behavior in systems, following the existing APIs and nearby patterns.

Keep XAML for UI layout and styling, and `.xaml.cs` code-behind for view state and translating control input into intent. A BUI/EUI adapter owns window lifecycle and state/message binding; Shared contains only the minimal serializable contract, while Server validates and applies authoritative changes. Follow `.agents/skills/xaml-ui/SKILL.md` and `.agents/skills/bound-user-interface/SKILL.md` for the specific UI type.

Prefer the owning system's public API over direct manager access. Do not use reflection, copied private logic, or cross-assembly partial classes to reach inaccessible state. If the required extension point is missing, identify the declaration and assembly boundary and stop before introducing a workaround.

For entity queries, lifecycle, timing, networking, prediction, and hot paths, use the corresponding skills from `.agents/skills` as routed by `.agents/CATALOG.md`. Verify behavior at the layer that owns it; a successful build alone does not prove runtime behavior.

The repository targets `net10.0` with C# 14. Use newer language features when they make intent clearer or support the API being added; do not rewrite working code only to adopt new syntax. Check `.agents/skills/csharp-style/SKILL.md` for C# 14 behavior that can affect overload resolution or property and extension declarations.

## Resources and localization

Use localized strings for player-visible text. English (`en-US`) defines localization keys and structure. Mirror structural changes in the corresponding Russian (`ru-RU`) file at the matching relative path, creating the counterpart when needed. Preserve key order, variables, selectors, attributes, and paths. Write natural Russian and do not use `THE(...)` wrappers.

Reuse existing prototypes, assets, sprite states, audio, maps, and locale files before adding new ones. Check attribution and license terms before importing third-party material. Follow the owner-specific resource and localization rules for validation.

## Formatting conventions

### YML style

For entity prototypes, keep everything consistent and sort the top level fields by `type > abstract > parent > id > name > suffix > description > placement > categories`.
For any other prototypes at least keep `type > abstract > parent > id` in order.

Unindent lists so they are in-line with whatever declared them
```yml
# Good
- type: entity
  components:
  - type: Sprite
    layers:
    - state: icon

# Bad
- type: entity
  components:
    - type: Sprite
      layers:
        - state: icon
```

Instead of copy pasting something in the same file 20 times, use anchors using `&` to define and `*` to reference blocks of data
```yml
- type: entity
  components:
  - type: EmitSoundOnUse
    sound: &sound
      path: /Audio/Items/honk.ogg
  - type: EmitSoundOnLand
    sound: *sound
  - type: EmitSoundOnTrigger
    sound: *sound
  - type: MeleeWeapon
    sound: *sound
```


## Markers and upstream sync

The marker rules are intentionally maintained in `.agents/rules/arcane-edit-markers.md`; consult that file instead of copying marker syntax into this guide. In short, Arcane markers record our edits to inherited files, while the owner path identifies Arcane-owned code and resources. Never author a change under another fork's marker.

For changes to inherited prototypes, prefer supported partial prototypes when they express the change cleanly. Verify the current resource path and schema before using or creating a partial. Do not reorder unrelated upstream entries or reformat neighboring content.

## Changelogs

Firstly do not make changelogs for irrelevant things players won't notice.
This means code refactors, extremely niche stuff, etc.

Changelogs support writing to several different changelog files per pr.
Start with `:cl:`, optionally followed by a custom name for the changelog, on the first line.
Then each changelog line should be like this:
```
- add: Added something.
- remove: Removed something.
- fix: Fixed something.
- tweak: Changed something.
```

For mapping PRs, only sweeping changes to the map pool should be in the main changelog. Smaller things go in the `MAPS` changelog, for example:
```
:cl:
MAPS:
- tweak: Bagel: Fixed door access in engi.
```

Other changelogs you may use include `ADMIN` and `RULES`. once you set it with `NAME:`, any changes following it will use that file unless you switch it later.
