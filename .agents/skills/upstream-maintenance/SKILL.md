---
name: upstream-maintenance
description: Keep inherited changes narrow, correctly marked, traceable, and easy to rebase across repository layers.
---

Before editing inherited code, determine whether the change is an upstream bug fix, reusable extension point, compatibility adaptation, or repository-owned behavior.

Verify the current repository owner tag and existing marker syntax.

Repository-owned behavior belongs in owner-local module or underscore paths whenever possible. Those paths do not receive redundant owner edit markers.

When an inherited file must change:

- add the current repository marker around the smallest changed block
- preserve nearby formatting and upstream layout
- avoid unrelated cleanup
- document intentional divergence and source revision when relevant
- replace an upstream marker on a line you change, since the line becomes yours; leave upstream markers on untouched lines alone

Separate mechanical upstream conflict resolution from repository behavior changes. Do not copy a whole inherited file merely to change a small branch when a supported extension point exists.

An upstream change is not ours by landing in our tree. If a feature came from TraumaStation and we do not want it as-is, rework it and mark the result with our marker; do not leave an upstream marker on a line we authored.

When a rebase or merge actually conflicts, the side with a current repository marker outranks the incoming side. Take the version without the marker when the incoming change does not invalidate the marked one, and ask when it does. Full priority order and marker forms: `.agents/rules/merge-conflict-resolution.md`.
