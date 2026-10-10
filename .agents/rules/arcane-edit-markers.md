# Arcane edit markers

Marking an inherited file is what makes an Arcane change survive the next upstream sync. A change that is not marked is a change that will be reverted by someone else's rebase.

Marking applies only outside owner-local paths. Arcane runtime projects are root-level `Content.Arcane.*`; Arcane-owned resource directories include existing `Resources/**/_Arcane/**` paths. `Modules/Arcane` currently contains guidance files, not runtime code.

## Which form to use

The form depends on whether the lines are new or changed, and on how many.

| Change | Form | Location |
|---|---|---|
| exactly one line added | inline `Arcane` | trailing on the added line, no `-Start` / `-End` |
| two or more lines added | `Arcane-Start` / `Arcane-End` | wraps the added lines |
| exactly one line changed | inline `Arcane-Edit` | trailing on the changed line |
| two or more lines changed, up to 5 | `Arcane-Edit-Start` / `Arcane-Edit-End` | wraps the changed lines |
| more than 5 lines changed | `Arcane-Edit-Start` / `Arcane-Edit-End` | wraps the changed lines; keep active code and data active |

The single-line case is the common one, because most changes touch one line. A single added line gets a bare trailing `Arcane`, never `Arcane-Start`:

```yaml
# one added line
  - SpeedLeftLeg # Arcane
```

```csharp
// one added line
private readonly int[] _cache = new int[16]; // Arcane
```

The moment a change covers two or more lines of the same kind, the block form is required. Do not use inline markers for each line of a multi-line change, and do not open a `-Start` / `-End` pair around one line.

`-Start` / `-End` are never trailing markers. A single added line is marked `Arcane`; `Arcane-Start` only appears as the opening half of a pair.

Added and modified never share a marker. A block that contains both a new line and a changed line is two blocks.

## Marker syntax as it exists

```
C#:    // Arcane
C#:    // Arcane-Edit: <old> > <new>
YAML:  # Arcane
YAML:  # Arcane-Edit: <old> > <new>
YAML:  # Arcane-Start
YAML:  # Arcane-Start: <reason>
YAML:  # Arcane-End
YAML:  # Arcane-Edit-Start
YAML:  # Arcane-Edit-Start: <reason>
YAML:  # Arcane-Edit-End
```

Inline `Arcane` and inline `Arcane-Edit` mark exactly one line, trailing on that line. Two or more lines of the same kind require a `-Start` / `-End` pair. A bare `-Start` never appears as a trailing marker.

Either `-Start` takes an optional `<reason>` suffix, and it means the same thing in both: why this block exists, for a reader who cannot tell from the lines alone. `# Arcane-Start: Syndicate` marks a list as Arcane's. Neither `-End` ever takes a colon.

Use the marker form above consistently in C# and YAML: one added line gets a trailing `Arcane`, multiple added lines get a paired block, one changed line gets a trailing `Arcane-Edit`, and multiple changed lines get a paired edit block.

A multi-line C# change still prefers an owner-local partial file when the addition is additive enough to live there. The marker requirement is separate from that choice: wherever the change does land, it gets the form its line count calls for.

```csharp
// two or more changed lines
// Arcane-Edit-Start: 1800 > 3000
// cost: 3000
// cooldown: 5
// Arcane-Edit-End
```

A bare `# Arcane` is the single-line addition form in YAML.

`# Arcane-Edit: <old> > <new>` records what Arcane replaced. Read `new`, not `old`: if a rebase offers `old`, the correct resolution is to restore `new`.

## Added usings

Every added `using` goes below all other `using` directives in the file, and inside a marker. One added `using` gets a trailing `// Arcane`; two or more get the pair.

```csharp
// one added using
using Content.Arcane.Shared.Atmos; // Arcane
```

```csharp
using Content.Shared.Atmos;
using Robust.Shared.Map;

// Arcane-Start
using Content.Arcane.Shared.Atmos;
using Content.Arcane.Shared.Map;
// Arcane-End
```

Two requirements at once: last position, and marked. A new `using` placed alphabetically among the existing ones is wrong even when it is marked, because it will collide with every upstream edit to the using block.

The current C# tree does not follow this yet. Arcane has never marked a `using` in C#, and existing fork usings use `// <Trauma>` / `// </Trauma>` or a trailing `// Trauma` instead. Those are upstream markers on lines we did not touch, so they stay. New Arcane usings follow the rule above.

## More than five modified lines

The line count changes the marker form, not whether the new behavior is enabled. Wrap changed lines in an `Arcane-Edit-Start` / `Arcane-Edit-End` block and leave working code or data active. Add a reason suffix when it helps a future sync understand the change.

Comment out removed entries only when disabling those entries is the intended behavior, the format supports comments, and preserving the old text is useful. Do not comment out a working replacement just to satisfy the marker rule.

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

This removal pattern is appropriate only when the commented entries are intentionally disabled. For a live replacement, keep the replacement active inside the marker block and preserve old values in a reason suffix when needed.

## Merge adjacent markers

Two changes of the same kind are one change when they touch neighbouring lines. Merge them into a single `-Start` / `-End` pair, whatever their sizes.

Merging keeps one hunk instead of several, which means one place for a rebase to conflict rather than several. Merging is mechanical, and say it happened in the report.

### Merge two pairs

```yaml
# Arcane-Start
itemOne: 1
itemTwo: 2
# Arcane-End

# Arcane-Start
itemThree: 3
itemFour: 4
# Arcane-End
```

becomes

```yaml
# Arcane-Start
itemOne: 1
itemTwo: 2
itemThree: 3
itemFour: 4
# Arcane-End
```

Changed lines merge the same way.

```yaml
# Arcane-Edit-Start
cost: 3000
cooldown: 5
# Arcane-Edit-End

# Arcane-Edit-Start
delay: 2
delayPerUnit: 0.5
# Arcane-Edit-End
```

becomes

```yaml
# Arcane-Edit-Start
cost: 3000
cooldown: 5
delay: 2
delayPerUnit: 0.5
# Arcane-Edit-End
```

### Merge inline markers into one pair

Two inline markers near each other are one change too, so put them in a single pair rather than marking each line separately.

```yaml
itemOne: 1 # Arcane

itemTwo: 2 # Arcane
```

becomes

```yaml
# Arcane-Start
itemOne: 1
itemTwo: 2
# Arcane-End
```

```yaml
cost: 3000 # Arcane-Edit: 1800 > 3000
cooldown: 5 # Arcane-Edit: 10 > 5
```

becomes

```yaml
# Arcane-Edit-Start
cost: 3000
cooldown: 5
# Arcane-Edit-End
```

Note what the merge costs in a rewrite. The inline form records `1800 > 3000` per line, and the block form has nowhere to put those values. When a future sync will need them, keep them in the reason suffix.

```yaml
# Arcane-Edit-Start: mining was too cheap to spam, was 1800
cost: 3000
cooldown: 5
# Arcane-Edit-End
```

### What does not merge

A lone added line wrapped in its own pair.

```yaml example="wrong"
# Arcane-Start
itemOne: 1
# Arcane-End
```

One added line is a bare trailing marker, not a block.

```yaml
itemOne: 1 # Arcane
```

A block and a lone line beside it. The block was a block because it covered two or more lines, and the lone line was never a block.

```yaml
# Arcane-Start
itemOne: 1
itemTwo: 2
# Arcane-End

itemThree: 3 # Arcane
```

## Never merge across change kinds

Two pairs of the same kind merge. Pairs of different kinds never merge.

Merge `Arcane-Start` into `Arcane-Start` and `Arcane-Edit-Start` into `Arcane-Edit-Start`. Never merge an `Arcane-Start` block into an `Arcane-Edit-Start` block, even when they are adjacent. The `-End` marker says which kind of change a reader is looking at, and a merged block cannot say that.

```yaml example="wrong"
# Arcane-Edit-Start
oldName: oldValue
# Arcane-Edit-End

# Arcane-Start
newName: newValue
# Arcane-End
```

stays two blocks. The rewrite and the addition are separate facts, and the next sync has to resolve them independently.

When reviewing a file, inspect its actual marker boundaries; do not rely on a remembered repository-wide inventory.

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

Do not repair an unrelated inherited marker defect as a side effect of another task. If a defect is in a file being changed, inspect its current context and fix only what is necessary for the requested change; report unrelated defects separately.

## Foreign markers are upstream markers

`Trauma`, `Goobstation`, `Goob`, `DeltaV`, `Shitmed`, and `EinsteinEngines` markers are not ours. They record upstream authorship; interpret them from the affected line and its history rather than assuming the marker identifies who authored a later change.

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
- one added line carries a trailing bare `Arcane`, never `Arcane-Start`
- several added lines are wrapped in `Arcane-Start` / `Arcane-End`
- one changed line carries a trailing `Arcane-Edit: <old> > <new>`
- several changed lines are wrapped in `Arcane-Edit-Start` / `Arcane-Edit-End`
- put a bare `-Start` or `-End` on a line rather than wrapping a pair
- do not rewrite upstream markers on lines we are not changing, and do not convert them in bulk as a side effect of unrelated work

The Trauma vocabulary has more forms than the Arcane one, because it describes a different job: `// Trauma - reason` for a single line, `// <Trauma>` / `// </Trauma>` for a block, `/* Trauma` ... `*/` for removing a section. Reproducing any of them on our own change is wrong even though the syntax is valid and appears thousands of times in the tree.

Placement differs by owner and file. Preserve upstream placement on untouched content; place Arcane additions according to the file structure and the rules in this document. Do not copy another fork's ordering convention as an Arcane requirement.

## Marking inside Trauma files

`Content.Trauma.*`, `Resources/_Trauma/**`, and `*.Trauma.cs` partials are not exempt. They are owner-local for Arcane, but they are also upstream surface for everyone syncing TraumaStation, so an unmarked Arcane change in them is indistinguishable from a Trauma change and gets reverted.

Mark Arcane changes in inherited Trauma files with Arcane markers:

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

Arcane-owned means no marker is needed for ownership. `Content.Arcane.*` and existing `Resources/**/_Arcane/**` paths do not sync from TraumaStation, so an Arcane marker there is unnecessary.

An `Arcane-Start` block inside a `_Trauma` file means these entries are Arcane's, not Trauma's. That is exactly the information a sync needs.

## Never do this

- add a marker inside `Content.Arcane.*` or an Arcane-owned `_Arcane` resource directory; the path already identifies ownership
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
