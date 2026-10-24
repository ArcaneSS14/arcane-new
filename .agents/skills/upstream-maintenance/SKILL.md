---
name: upstream-maintenance
description: Keep inherited changes narrow, correctly marked, traceable, and easy to rebase across repository layers.
---

## Когда применять

Use when an inherited path needs an upstream bug fix, extension point, compatibility adaptation, divergence, revert/restore analysis, or conflict resolution.

## До изменения

Classify the change as upstream bug fix, reusable extension point, compatibility adaptation, or repository-owned divergence. For add/remove/revert/restore, trace relevant history and actual diffs through `.agents/rules/change-history-analysis.md`; do not decide from a title or marker alone. Verify current owner tag and marker syntax.

## Правила и границы

- Prefer an owner-local module/underscore path for repository-owned behavior; those paths receive no redundant markers.
- For an inherited edit, use the smallest current-repository marker block, preserve nearby layout, avoid format cleanup, and document intentional divergence/source revision when relevant.
- Do not copy a whole inherited file for a small change when a supported extension point exists. If an upstream-marked line is changed, it becomes Arcane-marked; untouched upstream markers remain untouched.
- Separate mechanical conflict resolution from an Arcane behavior change. An upstream change landing in the tree is not ours until Arcane authors/adapts it with the correct marker.
- For a real conflict read base, current, and incoming. Follow `.agents/rules/merge-conflict-resolution.md`: the current Arcane marker outranks incoming; retain the unmarked version when incoming does not invalidate the marked change, and ask when intent remains unresolved. Never blanket-select `ours` or `theirs`.

## Выбор подхода

Use an upstream fix when it is the intended sync behavior, an extension point when it can isolate Arcane behavior, and an owner-local divergence when the repository intentionally differs. Keep source-history research proportional to the actual inherited path rather than scanning unrelated history.

## Антипаттерны

- Do not judge provenance from a marker/title, copy an entire inherited file, or mix formatting cleanup with a behavior change.
- Do not put redundant markers in owner-local paths, leave an upstream marker on an Arcane-authored line, or resolve a conflict mechanically.
- Do not call an unreviewed incoming change an Arcane change merely because it is present locally.

## Проверка

Inspect history/diffs required by the change, final marker placement, inherited diff scope, and delivery note for intentional divergence/source revision when relevant. Verify only the directly affected owner path; do not rebase, merge, or initialize engine dependencies for documentation-only work.

## References

No reference is required for ordinary inherited maintenance; read `.agents/rules/change-history-analysis.md` for add/remove/revert/restore history and `.agents/rules/merge-conflict-resolution.md` for an actual conflict.
