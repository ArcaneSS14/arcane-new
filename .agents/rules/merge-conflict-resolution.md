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

Choose [owner-local paths](architecture-and-ownership.md#owner-local-paths) before marking the resolution. For inherited files use the [canonical marker forms and contiguous-run boundaries](arcane-edit-markers.md#which-form-to-use); do not enlarge a block merely because the conflict hunk is larger.

When an Arcane change conflicts with an incoming change to the same line, compare their confirmed behavior. If incoming completely implements the Arcane intent, it may replace the older Arcane implementation; preserve or adapt the marker only when the resolved line remains Arcane-authored. If both sides are only partly equivalent, are behaviorally incompatible, or their intent cannot be proven, stop and ask for a decision.

The `old > new` marker records provenance; it does not settle a later behavioral change by itself. An incoming reintroduction of `old` is evidence to investigate, not permission to restore `new` without reading the current contract.

Foreign markers are not Arcane markers. `Trauma - ` and `Goobstation-` blocks in the tree record upstream authorship, not ours. They carry the same standing relative to upstream, but they are not this repository's vocabulary.

In inherited files, the marker must match who authored the resolved line. When the winning side is an upstream line we did not author, keep its upstream marker intact. Mark our changed lines using the canonical forms above; conflict resolution alone does not make unchanged upstream code ours.

An incoming Trauma change does not arrive as an Arcane change because it lands in our tree. Do not resolve a conflict by adopting upstream's line and re-marking it as ours, and do not resolve it by keeping a `Trauma - ` marker on a line we authored.

Do not add Arcane markers in owner-local paths, including the justified same-assembly and infrastructure exceptions in the placement table. A foreign underscore directory is not Arcane-owned: preserve upstream markers on unchanged lines, but mark our changes in inherited `_Trauma`, `_Shitmed` and other foreign paths.

### Trajectory

Syncs arrive along the Trauma trajectory. Before resolving, classify the file, because the same conflict carries a different cost in each class.

| File class | Cost of keeping our side |
|---|---|
| Arcane owner-local under the placement table | none; resolve normally |
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
- enum/flag extensions: compare numeric identities, intentional aliases/composite masks and serialized consumers; distinct names can collide. Preserve established values unless an agreed compatibility change requires otherwise.

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
