# Semantic conflict resolution

Use this rule for actual conflicts within an authorized integration. Git strategy changes follow [git-safety](git-safety.md); inspection alone does not authorize merge, rebase or pull.

## Establish the three versions

Read the base and both sides of each affected hunk. git show :1:<path>, :2:<path>, :3:<path> expose index stages: base, stage-2 side and stage-3 side. In a merge stage 2 is current HEAD and stage 3 incoming. During rebase the usual branch meaning of ours/theirs is reversed: identify actual refs/commits rather than trusting the words alone.

Compare callers, moved paths and relevant history when a hunk's intent is unclear. Do not infer behavior from commit titles, side length or marker presence alone. Classify the affected path with [trajectory](fork-trajectory-priority.md); Trauma paths and *.Trauma.cs are sync-sensitive, not Arcane owner-local.

## Preserve intent and accept compatible improvements

- Preserve confirmed Arcane intent and agreed observable behavior while adapting its implementation to compatible incoming APIs, fixes and structure.
- Arcane markers and old > new values identify divergence and history. They do not establish correctness, whole-file priority or automatic rejection of a security fix.
- Remove a former Arcane implementation without another question only when current-code evidence proves incoming fully provides the same agreed behavior, including the relevant consumers and lifecycle. Partial or assumed equivalence is insufficient.
- If intended behavior changes, intent is incompatible or available evidence cannot settle a correctness decision, ask the user and continue independent work. Sprite and map conflicts go to the user.
- Where both sides carry compatible meaning, integrate both at the correct current location. Do not concatenate incompatible handlers, average values or silently keep a stale implementation merely because it is marked.

## Authorship and boundaries

Preserve unchanged incoming text and its authorship markers. An Arcane-authored adaptation in an inherited path uses [Arcane markers](arcane-edit-markers.md); an unchanged incoming line is not re-marked as ours. Arcane owner-local paths need no marker. Keep paired boundaries valid, surrounding style and unrelated lines intact.

Preserve SPDX lines under [Git safety](git-safety.md). Do not use whole-file checkout --ours/--theirs or -X ours/theirs to bypass semantic resolution, or stage unread paths. An explicitly verified purely mechanical rename may retain an unchanged side only after comparing both contents and references.

## Verify and report

Inspect every resolved hunk; run git diff --check and search the resolved paths for remaining conflict delimiters. Check status and the smallest relevant behavior/resource contract under [verification](verification.md). Report each meaningful conflict's path, intents, resolution evidence and unresolved question; do not claim behavior equivalence from a clean merge alone.
