# Change history and provenance analysis

Use this rule when the requested work depends on what happened to a feature over time: porting or backporting it, restoring removed behavior, examining a revert or revert-of-revert, investigating a regression, or deciding whether an old implementation should remain deleted.

Do not turn a routine local change into a history audit. For the cases above, current code and a commit subject are not enough to establish intended behavior.

## Establish the source and scope

Record the source repository and exact source ref/commit, destination base/ref, and the feature paths or symbols under review. A branch name, PR title, or commit subject is a search clue, not a source revision or explanation.

Start with the narrowest useful history:

```sh
git log --all --follow --format='%H %P %s' -- path/to/file
git log --all --oneline -S'SymbolName' -- path/to/file
git log --all --oneline -G'pattern' -- path/to/file
git log --all --oneline --grep='distinctive phrase' --regexp-ignore-case -- path/to/feature
git show --format=fuller --stat --find-renames <commit>
git show --format= --find-renames <commit> -- path/to/file
```

Use `-S` for an exact string's addition/removal and `-G` for matching changed lines. Use `--follow` for a file rename and `--find-renames` when reviewing a patch. Keep searches path-scoped; expand only when evidence shows the code moved or when the requested feature spans multiple known files. A commit-message search does not prove that the commit changed the behavior in question.

For each important commit, record its full SHA, parents, changed paths, and actual diff. If it is a merge commit, identify the relevant parent and compare against that parent; do not assume the first-parent diff describes the whole change. For a PR family, inspect the source PR's base/head and follow-up commits when available, not only the squash or merge commit title.

## Reconstruct the timeline

For the affected behavior, identify and compare:

1. the code before introduction;
2. the introducing change and all related files;
3. follow-up fixes, adaptations, and partial removals;
4. each revert or revert-of-revert, including manual conflict resolutions;
5. the selected source state and the current destination state; inspect later source changes when the requested port is meant to represent current behavior.

For a suspected revert, inspect both the original patch and the revert patch. Compare their actual parent-to-commit diffs and then inspect the current file. A commit titled `Revert ...` may be partial, may revert a merge relative to a selected parent, or may be followed by a manual restoration. Do not assume the feature is absent, rejected, or safe to restore from the title alone.

For deletions or cuts, establish whether the code was removed because it was defective, replaced, moved, made redundant, or intentionally retired. Follow renamed symbols/files and inspect the replacement, callers, tests, prototypes, localization, assets, configuration, and project references in the same feature family. Use `git blame` only to locate context for current lines; it does not replace tracing earlier additions and removals.

## Decide what represents intended behavior

Treat commit diffs, tests, PR review, linked issues, changelogs, and the current source tree as evidence that must agree where possible. When the request targets current source behavior, prefer the latest accepted and maintained state, including fixes after the original port. When the user specifies a historical commit, honor that scope and report later changes that materially alter the behavior; do not silently widen the port. A revert explains an event in history; its rationale must be verified from the patch and available discussion. It does not by itself mean the feature should stay removed or should be restored.

Separate the behavior from its historical implementation. Adapt to the destination's current APIs, architecture, ownership, security model, and resources. Do not reintroduce a known bug, obsolete API, removed dependency, foreign-fork ownership, or stale marker merely because it appears in an earlier commit.

If evidence is missing or conflicting, state the uncertainty and its impact. Do not invent a revert reason or silently choose between incompatible behaviors. Continue independent work, but defer the behavior decision when it is necessary for correctness.

## Record the evidence

Before implementing a port or restoration, keep a compact timeline or inventory with:

- source repository and exact base/head or source revision;
- introducing commit/PR, relevant follow-ups, revert(s), and later restorations;
- added, modified, renamed, and deleted files in the feature family;
- intended final behavior and which evidence supports it;
- included/excluded changes and the reason for each meaningful exclusion;
- unresolved history gaps and verification that can prove the destination behavior.

Use `.agents/skills/porting/references/source-pr-family-manifest.md` for a port's family inventory. In the delivery note, report the verified source revision, material revert/fix history, omitted changes, and any uncertainty. Keep citations/links or SHAs precise enough for another contributor to reproduce the trace.

## Safe scope

Prefer existing local refs and path-scoped read-only Git commands. Do not reset, rewrite, cherry-pick, revert, merge, rebase, or add/change remotes merely to investigate history. If a required source commit is unavailable locally, report the gap and use the normal project workflow to obtain it only when that is within the task's authorization. History inspection does not authorize history mutation. Follow `.agents/rules/git-safety.md` and `.agents/skills/git-workflow/SKILL.md` if the requested work includes Git mutations.
