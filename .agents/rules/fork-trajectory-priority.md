
This policy helps choose an owner and keep Arcane changes easy to reconcile with the TraumaStation sync trajectory. It supplements the Arcane project layout in `CONTRIBUTING.md`; it does not require every feature to be upstream-compatible.

## Confirm whether sync rules apply

Treat `Content.Trauma.*`, `Resources/**/_Trauma/**`, `.Trauma.cs` files, and vanilla root content/resources as sync-sensitive. Before a sync or conflict task, inspect the actual remotes and commits. A remote named `upstream` does not by itself prove that it contains the TraumaStation sync source.

Other fork-owned paths, including `Content.Goobstation.*`, `Content.Lavaland.*`, `Resources/**/_Goobstation/**`, `Resources/**/_Lavaland/**`, and other underscore owners, are not Arcane-owned. Preserve their content and markers; do not use them as a destination for Arcane-only behavior.

## Choose the destination

| Path class | Examples | Use |
|---|---|---|
| Arcane-owned | root `Content.Arcane.*`, `Resources/**/_Arcane/**` | default for Arcane-only behavior; no Arcane edit marker |
| Trauma-owned | `Content.Trauma.*`, `Resources/**/_Trauma/**`, `*.Trauma.cs` | use when the change belongs on the Trauma-compatible trajectory; mark our edits with Arcane markers |
| Vanilla base | root `Content.*` not owned by a fork, `Resources` without an owner directory | change only when the extension cannot live in an owner project/resource path; keep the edit minimal and marked |
| Other fork | `Content.Goobstation.*`, `Content.Lavaland.*`, `Resources/**/_Goobstation/**`, etc. | preserve that fork's conventions and markers; do not place Arcane-only work there |

Prefer an Arcane-owned project/resource path for Arcane-only work. Use a Trauma-owned path only when compatibility with that code trajectory is an explicit design requirement or the supported architecture requires it. Do not choose a destination based only on fewer files or perceived sync cost.

## Keep inherited edits small

When a change must land in a Trauma-owned or vanilla path:

- first check whether an existing Arcane system, partial prototype, or other supported extension can own the behavior;
- limit the changed lines and keep the hunk contiguous;
- do not reformat or reorder neighboring code;
- prefer additive changes when they express the behavior correctly;
- use Arcane markers for our changes and preserve markers on untouched lines;
- replace a foreign/upstream marker only on a line we actually change.

See `.agents/rules/arcane-edit-markers.md` for exact syntax. Use `.agents/rules/merge-conflict-resolution.md` for an actual conflict; do not choose a sync or history strategy on the user's behalf.

## Arcane edits inside the Trauma trajectory

A change inside a Trauma-owned file can be correct when its ownership and compatibility intent justify it.

The requirement is not avoidance. It is that the edit stay cheap to reconcile. Ranked from cheapest to most expensive:

**1. Append, do not insert.** Add new list entries at the end of the list, inside one contiguous block. An appended block is one conflict point; entries sprinkled into an alphabetically sorted list are one conflict point each.

**2. One block, not many.** Group adjacent Arcane additions into one marker block when the marker syntax calls for a block. See `.agents/rules/arcane-edit-markers.md` for the exact threshold and supported syntax.

**3. Prefer additive over destructive.** Adding a key, tag, or component entry is easy to replay on conflict. Deleting or rewriting an upstream line collides with every nearby upstream edit. When both are needed, prefer the addition and achieve the removal through a partial or an `!Remove`.

**4. Leave neighbours alone.** Do not reformat, re-indent, reorder, re-alphabetize, or tidy anything the change does not require. A diff that touches three lines is a three-line diff even if the feature needs one.

**5. Keep the hunk contiguous.** A single hunk conflicts once. The same lines scattered across a file conflict several times.

**6. When a value must change, record both sides.** `Arcane-Edit: <upstream value> > <our value>` is what makes the edit replayable by hand after a conflict, because the upstream value is still written down.

Then, regardless of technique:

- mark the change with Arcane markers. An unmarked Arcane change inside a Trauma file is indistinguishable from a Trauma change and gets reverted by the next sync
- when we change a line carrying an upstream marker, replace it with an Arcane marker. The line is ours now, and keeping `Trauma - ` on it claims someone else's authorship for our change
- leave upstream markers on untouched lines alone, and never convert them in bulk
- do not nest an `Arcane-Start` block inside a `<Trauma>` block. Keep them sequential
- for a base file that already carries a `<Trauma>` using block at the top, put an Arcane using block last rather than inserting into the Trauma block
- report the file, the reason, and the technique used, in the delivery note

A small, marked addition to a Trauma file can be correct when that is the appropriate owner. The same feature pasted into a vanilla base path without justification or markers is not.

## Two marker vocabularies

Arcane markers and Trauma markers are different systems. The vocabulary is chosen by who wrote the change, not by which fork owns the file.

| Author | Single line | Block | Placement |
|---|---|---|---|
| Trauma | upstream-specific markers found in inherited files | upstream-specific blocks | preserve on lines we do not change |
| Arcane | syntax in `.agents/rules/arcane-edit-markers.md` | syntax in `.agents/rules/arcane-edit-markers.md` | mark only our additions and changes |

Never mix the two inside one block, and never nest one block inside the other.

Marker placement follows the file's existing structure and `.agents/rules/arcane-edit-markers.md`; do not copy another fork's placement rule as an Arcane rule.

Do not copy another fork's placement rule as an Arcane rule. Follow the file-format and placement guidance in `.agents/rules/arcane-edit-markers.md`.

`Goobstation-`, `<Goob>`, `DeltaV`, `Shitmed`, and `EinsteinEngines` markers are other forks' vocabularies. Preserve them, do not enforce them, do not convert them.

Before changing a sync-sensitive file, ask whether an Arcane-owned extension point can own the feature. Use it when it preserves the intended behavior and does not violate architecture.

## Prefer a partial over an edit

The established mechanism for keeping root-project code out of sync conflicts is a fork-suffixed partial file in the same directory.

```csharp
// Content.Shared/Atmos/MobStateSystem.cs          base, edited rarely
// Content.Shared/Atmos/MobStateSystem.Trauma.cs   owner-local, added freely
```

The suffix identifies the owner of the partial and can avoid changing a base file. Check for an established suffix and applicable scoped guidance before creating one.

Use it whenever the change does not require altering the base declaration. Declaring a partial method in the base file and filling it in a `.Trauma.cs` partial keeps the base diff down to the one attribute line.

When the base class is not already `partial`, adding a fork-suffixed partial may require a minimal edit to the base declaration. Mark that edit and explain why it is needed.

## Prefer an owner resource path over a base resource edit

Use Arcane-owned resource directories for Arcane resources. For a change to an existing upstream prototype, use a supported partial prototype when it expresses the change cleanly.

```yaml
# Resources/Prototypes/_Arcane/Partials/...   Arcane-owned, when supported by the current loader
# Resources/Prototypes/Access/...            vanilla, conflicts on every sync
```

A partial prototype can override selected fields and merge or remove components without copying the parent. Use one only when the current loader supports it and the partial expresses the desired change; it reduces direct base-file edits but still requires checking the prototype contract.

For migrations or other compatibility-sensitive data, verify the existing owner and migration workflow before choosing a destination.

## When the base must change

Sometimes there is no owner-local alternative: a prototype ID already exists upstream, a component already exists in `Content.Shared`, or a base file is the only registration point.

Then:

- change the smallest number of lines that makes the feature work
- mark according to `.agents/rules/arcane-edit-markers.md`
- put added `using` directives last in the using block, inside a marker
- do not reformat, reorder, or tidy anything else in the file
- report the base-path edit explicitly in the delivery note, naming the file and the reason

A base edit that is small, marked, and explained is correct. A base edit that is convenient and unmarked is the failure this rule exists to prevent.

## Never

- add an Arcane marker inside an Arcane-owned project or `_Arcane` resource directory
- leave an Arcane-authored change in `Content.Trauma.*`, `Resources/_Trauma/**`, `Content.Medical.*`, `Resources/_Shitmed/**`, or a vanilla root path unmarked
- mark our own change with `Trauma - ` or `<Trauma>`, whatever the surrounding file uses
- nest an Arcane block inside another fork's marker block, or interleave them
- add a marker inside another fork's underscore dir
- keep an upstream marker on a line we changed; it becomes `Arcane-Edit: <old> > <new>`
- rewrite upstream markers on lines we are not changing
- rewrite a foreign-fork file in root paths to match this fork's structure
- rebase, merge, or otherwise resolve history to reduce apparent conflicts
- place Arcane-only behavior in another fork's project or underscore directory
- paste a ported feature into a vanilla root path instead of an Arcane path; see `.agents/rules/port-destination.md`

## Conflicts during a sync

A sync conflict must be resolved semantically using the base and both sides. Consider relocating Arcane behavior to an owner-local path only if that preserves the intended behavior and dependency boundaries. Follow `.agents/rules/merge-conflict-resolution.md`; do not silently discard either side.

Full three-version resolution order: `.agents/rules/merge-conflict-resolution.md`.

## Verification

```powershell
git diff --stat -- <path>
git diff -- <path>
git grep -n "Arcane-Edit\|Arcane-Start" -- <path>
```

Before delivery, confirm:

- every changed inherited file has a stated reason for being changed rather than relocated
- every line we changed carries an Arcane marker, and no line we changed carries a `Trauma - `, `<Trauma>`, or other upstream marker
- no marker was added inside an Arcane-owned project or `_Arcane` resource directory
- upstream markers on lines we did not touch are untouched
- added `using` directives follow the placement rules in `.agents/rules/arcane-edit-markers.md`
- `git diff --check` is clean

State inherited files edited and the reason their behavior could not live in an Arcane-owned path.

Adding or changing a remote changes repository configuration; propose it rather than running it unless the user explicitly asks.
