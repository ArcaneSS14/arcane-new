# Merge conflict resolution

A conflict is a decision about which future is real. Resolve it deliberately, never by picking a side.

## The three versions

Every conflicted hunk has three versions. Identify all three before touching the file.

| Version | Meaning | In a merge | In a rebase |
|---|---|---|---|
| base | common ancestor, the last agreement | `git merge-base HEAD <incoming>` | same commit, different side |
| theirs | the incoming side, what you are pulling | `theirs` | `ours` |
| ours | your current branch, the future being pushed | `ours` | `theirs` |

The labels swap under rebase. Never reason from the words "ours" and "theirs" alone; read the ref names printed in the conflict markers.

Read all three explicitly:

```powershell
git diff --ours -- <path>
git diff --theirs -- <path>
git show :1:<path>
git show :2:<path>
git show :3:<path>
```

Stages are `1` base, `2` ours, `3` theirs, during a merge.

## Decision process

When the three versions disagree, establish the behavior and intent each version represents before choosing a result. An Arcane marker is provenance evidence, not a mechanical winner: it identifies a deliberate divergence that must be understood and either preserved, superseded by a fully equivalent incoming implementation, or escalated.

Marker forms, by file type:

```
YAML:  # Arcane                                    one added line
YAML:  # Arcane-Edit: <old> > <new>                  one changed line
C#:    // Arcane-Edit: <old> > <new>                 one changed line
YAML:  # Arcane-Start / # Arcane-End                 two or more added lines
YAML:  # Arcane-Edit-Start / # Arcane-Edit-End       two or more changed lines
YAML:  # Arcane-Edit-Start: <reason>                 edit block with reason
```

One line gets a trailing inline marker, bare `Arcane` for an addition and `Arcane-Edit: <old> > <new>` for a change. Two or more lines of the same kind get a `-Start` / `-End` pair. A bare `-Start` or `-End` never appears on its own line as a trailing marker.

The reason suffix is optional and used when the block's purpose is not obvious. `# Arcane-Edit-End` never carries a colon.

No C# block form exists in the tree yet, but the rule covers C#: a merged C# conflict touching two or more lines uses `// Arcane-Edit-Start` / `// Arcane-Edit-End`. When a conflict offers a larger Arcane block than the change actually needs, keep the block minimal and say so.

When an Arcane change conflicts with an incoming change to the same line, compare their confirmed behavior. If incoming completely implements the Arcane intent, it may replace the older Arcane implementation; preserve or adapt the marker only when the resolved line remains Arcane-authored. If both sides are only partly equivalent, are behaviorally incompatible, or their intent cannot be proven, stop and ask for a decision.

The `old > new` marker records provenance; it does not settle a later behavioral change by itself. An incoming reintroduction of `old` is evidence to investigate, not permission to restore `new` without reading the current contract.

Foreign markers are not Arcane markers. `Trauma - ` and `Goobstation-` blocks in the tree record upstream authorship, not ours. They carry the same standing relative to upstream, but they are not this repository's vocabulary.

The rule for any conflicted line: the marker must match who authored the resolved line. Our resolution is Arcane's, so it carries an Arcane marker. When the winning side is an upstream line we did not author, keep its upstream marker intact. When we changed that line, the resolved line is ours and carries `Arcane-Edit: <upstream value> > <our value>`.

An incoming Trauma change does not arrive as an Arcane change because it lands in our tree. Do not resolve a conflict by adopting upstream's line and re-marking it as ours, and do not resolve it by keeping a `Trauma - ` marker on a line we authored.

Arcane owner-local paths are the root-level `Content.Arcane.*` projects and existing `_Arcane` resource directories. Do not add Arcane markers there. Existing markers under another fork's underscore directory belong to that fork; preserve them and do not add new Arcane markers there.

### Trajectory

Syncs arrive along the Trauma trajectory. Before resolving, classify the file, because the same conflict carries a different cost in each class.

| File class | Cost of keeping our side |
|---|---|
| Arcane owner-local, root `Content.Arcane.*` or `_Arcane` resources | none; resolve normally |
| Trauma-owned, `Content.Trauma.*`, `Resources/**/_Trauma/**`, or `*.Trauma.cs` | sync-sensitive; preserve confirmed intent and mark Arcane-authored resolution |
| vanilla space-station-14, unmarked | high; this is the conflict surface |
| foreign fork path, `Content.Goobstation.*`, `_Goobstation`, `_DV`, `_EinsteinEngines` | low; not part of this trajectory |

For a vanilla root-path conflict, consider whether the local behavior can move to an Arcane-owned extension. Relocate it only when the move preserves behavior and dependency boundaries. Otherwise merge both sides semantically and explain the resolution.

For a foreign-fork-path conflict, resolve on its own terms. Do not import the Trauma minimization pressure onto a file that the trajectory never touches.

Classification details: `.agents/rules/fork-trajectory-priority.md`.

### Semantic merge when both carry meaning

When both sides changed the same thing for a reason, produce the third version that keeps both intents. Do not concatenate hunks and do not average conflicting values.

- full equivalence: keep the implementation that is current, supported, and preserves the confirmed intent; remove superseded Arcane code when it adds no behavior
- additive changes on both sides: keep both in the order the file establishes
- different hunks, same intent: keep one simplest verified form
- same key, different value: choose by confirmed behavior and record why
- structural move plus content change: apply the content change at the new location, preserving both confirmed intents

A merge that resolves without a single thought is a merge resolved wrong.

## What must survive

Independent of which side wins:

- `SPDX` lines. Never add, remove, reorder, normalize, or generate one during conflict resolution unless the user provides that exact change. Resolve the surrounding lines and leave the header alone.
- surrounding formatting and indentation. Take the merged content in the style of the file, not of the winning side.
- existing foreign edit markers and their pairs.
- comment intent. If both sides rewrote a comment, keep the one that explains the current code.
- both marker pairs intact. A conflict inside a `-Start`/`-End` region must resolve inside it.

## Never do this

- `git checkout --ours -- <path>` or `--theirs` across a whole file, to clear markers fast
- `-X ours` or `-X theirs` on the merge command, for the same reason
- resolving without reading the base
- deleting a marker pair because the block it wrapped is gone upstream
- reformatting an unrelated line while already in the file
- picking a winner because one side is shorter
- leaving a conflict marker in a committed file

Whole-file side selection is defensible only for a purely mechanical rename that both sides already agree on, and even then only after reading the diff.

## Verification

```powershell
git diff --check
git grep -n "^<<<<<<<\|^=======$\|^>>>>>>>" -- <resolved paths>
git status --short
git diff -- <path>
```

Confirm no conflict markers survive, that unrelated files are untouched, and that every resolved hunk is one you can justify in one sentence. Then re-run the smallest verification covering the resolved files.

Report every conflict you resolved as: file, what each side wanted, which rule decided it, and anything you escalated.

## Do not choose the policy

Whether to merge, rebase, or pull with a strategy is the user's decision. When a push fails because the remote moved, report it and stop. Do not run `git rebase`, `git merge`, or `git pull` with strategy flags to resolve a moving branch.

Read-only conflict inspection is always safe. `git diff`, `git show :1:`, `git log`, `git merge-base`, and `git status` are the tools for this rule.
