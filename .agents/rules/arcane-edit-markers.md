# Arcane edit markers

Marking an inherited file is what makes an Arcane change survive the next upstream sync. A change that is not marked is a change that will be reverted by someone else's rebase.

Marking applies only outside owner-local paths. Inside `Modules/Arcane`, `Resources/Prototypes/_Arcane`, and other Arcane-owned paths the path is the marker.

## Which form to use

The form depends on whether the lines are new or changed.

| Change | Form | Location |
|---|---|---|
| lines added | `Arcane-Start` / `Arcane-End` | wraps the added lines |
| one line changed | inline `Arcane-Edit` | trailing on the changed line |
| several lines changed, up to 5 | `Arcane-Edit-Start` / `Arcane-Edit-End` | wraps the changed lines |
| more than 5 lines changed | `Arcane-Edit-Start` / `Arcane-Edit-End` | wraps the changed lines, all commented out |

Added and modified never share a marker. A block that contains both a new line and a changed line is two blocks.

## Marker syntax as it exists

```
C#:    // Arcane-Edit: <old> > <new>
YAML:  # Arcane-Edit: <old> > <new>
YAML:  # Arcane-Start
YAML:  # Arcane-End
YAML:  # Arcane-Edit-Start
YAML:  # Arcane-Edit-Start: <reason>
YAML:  # Arcane-Edit-End
```

`# Arcane-Edit-Start: <reason>` explains why the change exists when the block's purpose is not obvious. `# Arcane-Edit-End` never takes a colon.

There is no C# block form in the tree. In C# an inline `// Arcane-Edit: <old> > <new>` is the only established marker, and a multi-line C# change goes into an owner-local partial file instead. Ask before introducing a C# block form.

`# Arcane-Edit: <old> > <new>` records what Arcane replaced. Read `new`, not `old`: if a rebase offers `old`, the correct resolution is to restore `new`.

## Added usings

Every added `using` goes below all other `using` directives in the file, and inside a marker.

```csharp
using Content.Shared.Atmos;
using Robust.Shared.Map;

// Arcane-Start
using Content.Arcane.Shared.Atmos;
// Arcane-End
```

Two requirements at once: last position, and marked. A new `using` placed alphabetically among the existing ones is wrong even when it is marked, because it will collide with every upstream edit to the using block.

Note that the current C# tree does not yet follow this. Arcane has never marked a `using` in C#, and the existing fork usings use `// <Trauma>` / `// </Trauma>` or a trailing `// Trauma` comment instead. Arcane follows the rule above, and those upstream markers stay as they are on lines we did not touch.

## More than five modified lines

When a change touches more than 5 existing lines, comment every changed line out and wrap the block, keeping the original values visible so the next sync can read what was replaced.

```yaml
# Arcane-Edit-Start
# FloorTileItemGrayConcrete: FloorTileItemConcrete
# FloorTileItemOldConcrete: FloorTileItemConcrete
# FloorTileItemIronsandConcrete: FloorTileItemConcrete
# AsteroidRockBananium: AsteroidRock
# AsteroidRockBananiumCrab: AsteroidRock
# WallSpawnAsteroidBananium: null
# WallSpawnAsteroidBananiumCrab: null
# Arcane-Edit-End
```

This is the pattern already used for the largest `migration.yml` edits. Below 5 lines the block stays live and uncommented; above 5 the block is disabled and commented, which is also how the surrounding file has been handled for years.

Reason the threshold: a live multi-line replacement is a silent behavioral change that no reader can diff. A commented one is inert and visible.

## Merge adjacent markers

Two marker blocks separated only by blank lines are one block. Merge them into a single `-Start` / `-End` pair.

```yaml
# Arcane-Start
itemOne: 1
# Arcane-End

# Arcane-Start
itemTwo: 2
# Arcane-End
```

becomes

```yaml
# Arcane-Start
itemOne: 1
itemTwo: 2
# Arcane-End
```

Two pairs in current use of this shape, both from an incomplete earlier merge:

- `Resources/Prototypes/Tiles/tile_migrations.yml`, `Arcane-End` at 81 followed by `-Start` at 83
- `Resources/Prototypes/_Goobstation/Entities/Objects/Weapons/Melee/justice.yml`, line 220 to 222

Merging is mechanical. Say it happened in the report.

Never merge across an `-End` and a `-Edit-Start`. Those are different change kinds and must stay separate markers.

## Verification

```powershell
git grep -n "Arcane-Start\|Arcane-End\|Arcane-Edit" -- <path>
```

Check, per file:

- `-Start` and `-End` counts match
- no nested `-Start` inside an open `-Start`
- no `Arcane-Edit` in `Arcane-Start` blocks, or the reverse
- no two marker blocks adjacent but unmerged
- added `using` lines are last in the file's `using` block and marked

An `-Edit-Start` with no matching `-End` is a real defect, not a style question. It means the block runs to end of file and the next sync has no boundary to work with.

Current known defects, all reported rather than fixed, since they are inherited:

- 7 files with an unclosed `Arcane-Edit-Start`, where the block runs to end of file because the change disables a prototype rather than relocating it: `Resources/Prototypes/Entities/Structures/Walls/malign.yml`, `Resources/Prototypes/Entities/Structures/Windows/malign.yml`, `Resources/Prototypes/_Arcane/Entities/Objects/Tiles/astro.yml`, `Resources/Prototypes/_Goobstation/Entities/Structures/Walls/asteroid.yml`, `Resources/Prototypes/_Lavaland/Entities/Structures/Walls/asteroid.yml`, `Resources/Prototypes/_Trauma/Partials/Entities/Structures/Doors/turnstile.yml`, `Resources/Prototypes/_Trauma/Tiles/astro.yml`
- `Resources/migration.yml`, two orphan `Arcane-Edit-End` at lines 1426 and 1431, left by a half-applied merge

Fixing inherited markers is a scope decision for the user, not a side effect of another task.

## Foreign markers are upstream markers

`Trauma`, `Goobstation`, `Goob`, `DeltaV`, `Shitmed`, `EinsteinEngines` markers are not ours. They record upstream authorship. There are 2222 `Trauma - ` and 2497 `<Trauma>` uses in the tree, plus 57 `/* Trauma` block removals, against 65 `Arcane-Edit:` and 58 `Arcane-Start`.

The rule that follows from this is absolute:

**Every change we author is marked with Arcane markers only. Never with `Trauma - `, never with `<Trauma>`.**

A Trauma marker on a line means Trauma wrote that line. When we change that line, the line becomes ours and the marker becomes `Arcane-Edit`, recording what we replaced:

```yaml
# before, upstream
cost: 3000 # Trauma - was 1800, its useless for mining
# after, ours
cost: 6000 # Arcane-Edit: 1800 > 3000
```

That is a legitimate rewrite of an upstream marker, and it is required rather than forbidden: leaving `Trauma - ` on a line we authored claims someone else's authorship for our change and hides our divergence from the next sync.

So the asymmetry runs the other way from the usual ownership reflex:

- a line we did not touch keeps its upstream marker, untouched
- a line we touched carries an Arcane marker, replacing whatever was there
- a line we added carries `Arcane-Start` / `Arcane-End`
- do not rewrite upstream markers on lines we are not changing, and do not convert them in bulk as a side effect of unrelated work

The Trauma vocabulary has more forms than the Arcane one, because it describes a different job: `// Trauma - reason` for a single line, `// <Trauma>` / `// </Trauma>` for a block, `/* Trauma` ... `*/` for removing a section. Reproducing any of them on our own change is wrong even though the syntax is valid and appears thousands of times in the tree.

Placement differs too, and the difference is deliberate: an upstream block goes where `CONTRIBUTING.md` puts it, first in a list, and an Arcane block goes last, so the two never collide. `CONTRIBUTING.md` puts `<Trauma>` at the top of a `using` block with upstream's imports following.

## Marking inside Trauma files

`Content.Trauma.*`, `Resources/_Trauma/**`, and `*.Trauma.cs` partials are not exempt. They are owner-local for Arcane, but they are also upstream surface for everyone syncing TraumaStation, so an unmarked Arcane change in them is indistinguishable from a Trauma change and gets reverted.

Mark the Arcane change there, with Arcane markers. Eleven Trauma-owned files already do this:

```csharp
// Content.Trauma.Server/Heretic/Systems/PathSpecific/AristocratSystem.cs
private static readonly EntProtoId IceWallPrototype = "WallRockSnow"; // Arcane-Edit: WallIce > WallRockSnow
```

```yaml
# Resources/Prototypes/_Trauma/Catalog/selectable_sets.yml
# Arcane-Start: Syndicate
```

```yaml
# Resources/Prototypes/_Trauma/Research/robotics.yml
# Arcane-Start
  - SpeedLeftLeg
  - SpeedRightLeg
# Arcane-End
```

Owner-local for Arcane means no marker is *needed for ownership*, but a marker is still *required* whenever the line is a divergence from TraumaStation. `Modules/Arcane/**` and `Content.Arcane.*` are the only paths where a marked line is genuinely wrong, because nothing syncs into them.

An `Arcane-Start` block inside a `_Trauma` file means these entries are Arcane's, not Trauma's. That is exactly the information a sync needs.

## Never do this

- add a marker inside `Modules/Arcane/**`, `Content.Arcane.*`, or `Resources/_Arcane/**`; nothing syncs there, so a marker is noise
- mark our own change with `Trauma - `, `<Trauma>`, or `/* Trauma`, whatever the surrounding file uses
- add a marker inside another fork's underscore dir
- leave an Arcane-authored change inside a Trauma or vanilla file unmarked
- keep an upstream marker on a line we changed. Replacing `Trauma - ` with `Arcane-Edit: <upstream value> > <our value>` is the correct result, not an unauthorized conversion
- rewrite upstream markers on lines we are not changing, and never convert them in bulk as a side effect of unrelated work
- mark a line that is not actually changed
- silently repair or normalize a marker in an inherited file as part of unrelated work
- change an `SPDX` line while marking
- leave an unbalanced `-Start` / `-End`
- use an Arcane marker to claim a change authored by another fork

Marking a divergence is not an excuse to edit a file. Before marking, check whether the change belongs in an Arcane-owned path at all: `.agents/rules/fork-trajectory-priority.md`.
