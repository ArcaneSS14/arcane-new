
`arcane-new` is a fork of TraumaStation. TraumaStation is a fork of GoobStation and of space-station-14. GoobStation is the same code lineage, not the base.

The sync source is TraumaStation. Every local edit to a Trauma-owned path or to a vanilla space-station-14 path is an edit the next TraumaStation import will have to reconcile.

Minimize those edits. Always.

## Authoritative source

`CONTRIBUTING.md` in this repository is the inherited TraumaStation contribution guide, and it is the authority on how Trauma-owned code is written. Read it before touching anything on the Trauma trajectory. It already mandates, in its own words:

- all new C# in `Content.Trauma.*` (rule 1)
- new methods and fields in base files as `.Trauma.cs` partials, making the class `partial` with a comment if it is not already (rule 4)
- no new event handlers on upstream systems; make a system in `Content.Trauma.*` instead (rule 5)
- resources under a `_Trauma` subdirectory (Resources)
- partial prototypes in `Resources/Prototypes/_Trauma/Partials` instead of editing upstream YAML (Partial Prototypes)
- single-line changes as `// Trauma - explanation` or `# Trauma - explanation`, multi-line as `// <Trauma>` / `// </Trauma>` (Commenting changes)

This rule adds the layer Trauma's guide does not cover: which of those rules applies to an Arcane-authored change, and where an Arcane change should go when both conventions are available.

## Trajectory order

Resolve where a change belongs in this order and stop at the first row that fits.

| Priority | Path class | Examples | Treatment |
|---|---|---|---|
| 1 | Arcane owner-local | `Modules/Arcane/**`, `Content.Arcane.*`, `Resources/_Arcane/**` | for Arcane-only behavior; never marked, nothing syncs there |
| 2 | Trauma owner-local, new | `Content.Trauma.*`, `Resources/_Trauma/**`, `*.Trauma.cs` | the default home for anything that does not need a base edit, per `CONTRIBUTING.md`. Arcane changes here are marked with Arcane markers |
| 3 | vanilla space-station-14, unmarked | `Content.Shared/Atmos/**`, `Resources/Prototypes/Access/**` | edit only when needed, cheapest-possible diff, marked |
| 4 | Trauma owner-local, existing upstream file | existing `Content.Trauma.*` files, upstream `Resources/_Trauma/**` files | edit freely when needed, cheapest-possible diff, Arcane markers |
| 5 | synced foreign fork | `Content.Medical.*`, `Resources/_Shitmed/**` | minimize. these carry incoming work, so keep diffs small and use the file's own marker |
| 6 | inert foreign fork | `Modules/GoobStation/**`, `Resources/_Goobstation/**`, `Resources/_DV/**`, `Resources/_EinsteinEngines/**`, `Resources/_Corvax*/**`, `Resources/_ADT/**`, `Resources/_Lavaland/**`, `Content.Goobstation.*` | relaxed; edit freely, keep foreign markers intact |

Rows 3 and 4 are the conflict surface. Row 5 is a weaker version of the same problem: Shitmed and the medical projects receive incoming work, so an edit there still gets reconciled, just less often.

Row 6 is inert with respect to this trajectory. Nothing syncs into those paths, so a change there does not fight the next import, and the minimization requirement is deliberately dropped there rather than merely softened.

Rows 1 and 2 exist to absorb the changes that would otherwise land in rows 3, 4, and 5. Prefer row 2 for a feature that belongs upstream over row 1: `CONTRIBUTING.md` rule 1 puts all new C# in `Content.Trauma.*`, and an upstream-compatible feature in `Content.Trauma.*` survives a sync, while the same feature in `Modules/Arcane` does not.

## Arcane edits inside the Trauma trajectory

A change inside a Trauma-owned file is a legitimate outcome. Do not refuse it and do not route around it into a more expensive path. `Content.Trauma.*` is ours, and there are changes that only make sense there.

The requirement is not avoidance. It is that the edit stay cheap to reconcile. Ranked from cheapest to most expensive:

**1. Append, do not insert.** Add new list entries at the end of the list, inside one contiguous block. An appended block is one conflict point; entries sprinkled into an alphabetically sorted list are one conflict point each.

**2. One block, not many.** Two or more added lines go into a single `Arcane-Start` / `Arcane-End` region rather than being scattered. A single added line gets a bare trailing `Arcane`. `CONTRIBUTING.md` gives the same reason for putting its own additions in one block.

**3. Prefer additive over destructive.** Adding a key, tag, or component entry is easy to replay on conflict. Deleting or rewriting an upstream line collides with every nearby upstream edit. When both are needed, prefer the addition and achieve the removal through a partial or an `!Remove`.

**4. Leave neighbours alone.** Do not reformat, re-indent, reorder, re-alphabetize, or tidy anything the change does not require. A diff that touches three lines is a three-line diff even if the feature needs one.

**5. Keep the hunk contiguous.** A single hunk conflicts once. The same lines scattered across a file conflict several times.

**6. When a value must change, record both sides.** `Arcane-Edit: <upstream value> > <our value>` is what makes the edit replayable by hand after a conflict, because the upstream value is still written down.

Then, regardless of technique:

- mark the change with Arcane markers. An unmarked Arcane change inside a Trauma file is indistinguishable from a Trauma change and gets reverted by the next sync. Eleven Trauma-owned files already carry `Arcane-Edit` or `Arcane-Start` for exactly this reason
- when we change a line carrying an upstream marker, replace it with an Arcane marker. The line is ours now, and keeping `Trauma - ` on it claims someone else's authorship for our change
- leave upstream markers on untouched lines alone, and never convert them in bulk
- do not nest an `Arcane-Start` block inside a `<Trauma>` block. Keep them sequential
- for a base file that already carries a `<Trauma>` using block at the top, put an Arcane using block last rather than inserting into the Trauma block
- report the file, the reason, and the technique used, in the delivery note

A three-line marked addition appended to a Trauma file is a good change. The same feature pasted into `Content.Shared` as forty scattered unmarked lines is not.

## Two marker vocabularies

Arcane markers and Trauma markers are different systems. The vocabulary is chosen by who wrote the change, not by which fork owns the file.

| Author | Single line | Block | Placement |
|---|---|---|---|
| Trauma | `// Trauma - <reason>` | `// <Trauma>` / `// </Trauma>` | top of the list, per `CONTRIBUTING.md` |
| Arcane | `// Arcane-Edit: <old> > <new>` | `// Arcane-Start` / `// Arcane-End` | bottom of the list, after upstream entries |

Never mix the two inside one block, and never nest one block inside the other.

The different placement is deliberate. `CONTRIBUTING.md` puts the Trauma block at the top of a `using` block or a component list, so an Arcane block at the top would collide with it on every sync. Arcane goes last instead.

When a file already carries a `<Trauma>` block at the top, an Arcane block belongs at the bottom, and they never interleave.

`Goobstation-`, `<Goob>`, `DeltaV`, `Shitmed`, and `EinsteinEngines` markers are other forks' vocabularies. Preserve them, do not enforce them, do not convert them.

Before touching a row 3, 4, or 5 file, answer: can this live in row 1 or row 2? Most of the time it can.

## Measure before editing

Of 18958 `.cs` and `.yml` files:

| Class | Files |
|---|---|
| vanilla space-station-14, unmarked | 9480 |
| vanilla space-station-14, marked by some fork | 1601 |
| Trauma owner-local | 2705 |
| underscore owner dir | 3287 |
| GoobStation | 1394 |
| Medical | 296 |
| Lavaland | 185 |
| Arcane | 10 |

Half the repository is row 3, which is what makes "cheapest possible diff" matter there rather than "do not touch". `Content.Arcane.*` holding only 10 files is a finding, not an anomaly: most changes still belong upstream-compatible enough to live in `Content.Trauma.*`.

## Prefer a partial over an edit

The established mechanism for keeping root-project code out of sync conflicts is a fork-suffixed partial file in the same directory.

```csharp
// Content.Shared/Atmos/MobStateSystem.cs          base, edited rarely
// Content.Shared/Atmos/MobStateSystem.Trauma.cs   owner-local, added freely
```

There are 250 `.Trauma.cs` partials against 10 `.Goob.cs`, 2 `.Shitmed.cs`, 1 `.Lavaland.cs`, and 1 `.DeltaV.cs`. The suffix names the owning trajectory and no incoming edit will collide with it, because no upstream file is named that.

Use it whenever the change does not require altering the base declaration. Declaring a partial method in the base file and filling it in a `.Trauma.cs` partial keeps the base diff down to the one attribute line.

`CONTRIBUTING.md` rule 4 adds one requirement that is easy to miss: when the base class is not already `partial`, the base file must be edited to make it partial, and that edit needs a comment. That is a legitimate, unavoidable base edit.

## Prefer an underscore dir over an edit

Same logic for resources. `CONTRIBUTING.md` requires resources under a `_Trauma` subdirectory, and for prototype changes it requires a partial prototype.

```yaml
# Resources/Prototypes/_Trauma/Partials/...   owner-local, the required form
# Resources/Prototypes/Access/...            vanilla, conflicts on every sync
```

A partial prototype overrides only the fields you name, merges components, and supports `!Remove` and `!Clear`, so it expresses most changes without copying the parent. `CONTRIBUTING.md` says this should be done except in special cases, and calls it preferable because it makes conflicts impossible.

`Content.Server.Database/Migrations/` holds 331 vanilla files and every migration in it conflicts with upstream. Put new schema work in the Arcane-owned project instead when a project exists for it.

## When the base must change

Sometimes there is no owner-local alternative: a prototype ID already exists upstream, a component already exists in `Content.Shared`, or a base file is the only registration point.

Then:

- change the smallest number of lines that makes the feature work
- mark per `.agents/rules/arcane-edit-markers.md`: one added line is a bare trailing `Arcane`, two or more added lines use `Arcane-Start` / `Arcane-End`, one changed line is a trailing `Arcane-Edit`, two or more changed lines use `Arcane-Edit-Start` / `Arcane-Edit-End`
- put added `using` directives last in the using block, inside a marker
- do not reformat, reorder, or tidy anything else in the file
- report the base-path edit explicitly in the delivery note, naming the file and the reason

A base edit that is small, marked, and explained is correct. A base edit that is convenient and unmarked is the failure this rule exists to prevent.

## Never

- add any marker inside `Modules/Arcane/**`, `Content.Arcane.*`, or `Resources/_Arcane/**`; nothing syncs there
- leave an Arcane-authored change in `Content.Trauma.*`, `Resources/_Trauma/**`, `Content.Medical.*`, `Resources/_Shitmed/**`, or a vanilla root path unmarked
- mark our own change with `Trauma - ` or `<Trauma>`, whatever the surrounding file uses
- place an Arcane block at the top of a list, where the Trauma block already lives
- nest an Arcane block inside a `<Trauma>` block, or interleave them
- add a marker inside another fork's underscore dir
- keep an upstream marker on a line we changed; it becomes `Arcane-Edit: <old> > <new>`
- rewrite upstream markers on lines we are not changing
- rewrite a foreign-fork file in root paths to match this fork's structure
- rebase, merge, or otherwise resolve history to reduce apparent conflicts
- touch `Modules/GoobStation/**` to place Arcane-only behavior; that module is inherited by Goob Reforged and its diff must stay comparable to upstream
- paste a ported feature into a vanilla root path instead of an Arcane path; see `.agents/rules/port-destination.md`

## Conflicts during a sync

A conflict in a row 3, 4, or 5 file is a self-inflicted cost, so resolve it by removing the local divergence wherever that is honest: take the incoming version and relocate the feature to row 1 or row 2. That is the first option, not the only one. When the divergence is genuinely required there, keep it, resolve the conflict by the priority order, and escalate rather than silently dropping our behavior.

A conflict in row 6 is not a sync cost from this trajectory. Resolve it on its own terms, keeping foreign markers intact.

Full three-version resolution order: `.agents/rules/merge-conflict-resolution.md`.

## Verification

```powershell
git diff --stat -- <path>
git diff -- <path>
git grep -n "Arcane-Edit\|Arcane-Start" -- <path>
```

Before delivery, confirm:

- every changed row 3, 4, or 5 file has a stated reason for being changed rather than relocated
- every line we changed carries an Arcane marker, and no line we changed carries a `Trauma - `, `<Trauma>`, or other upstream marker
- no marker was added inside row 1, where nothing syncs
- upstream markers on lines we did not touch are untouched
- an Arcane `using` block is last in the using block, a Trauma block is first, and the two are not interleaved or nested
- `git diff --check` is clean

State the count of row 3, 4, and 5 files edited, and for each one the technique used to keep it cheap. A count above zero is normal; an unmarked edit above zero is not.

## Identified remotes are not the sync source

`git remote -v` currently shows `origin` as `ReWAFFlution/cosplaytele.com` and `upstream` as `ArcaneSS14/arcane-new`. Both point at Arcane itself; neither is TraumaStation, and no TraumaStation remote is configured.

`git fetch upstream` therefore does not bring TraumaStation changes. Do not assume a sync is complete because `upstream` is up to date, and do not pick a merge or rebase policy from a ref that does not carry the Trauma trajectory. Ask which ref is the TraumaStation import before resolving sync conflicts.

Adding a remote changes repository configuration; propose it rather than running it.
