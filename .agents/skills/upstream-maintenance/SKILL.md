---
name: upstream-maintenance
description: Keep inherited changes narrow, correctly marked, traceable, and easy to rebase across repository layers.
---

# Maintain an inherited delta

Establish the actual upstream refs and the confirmed Arcane behavior to preserve. Distinguish an upstream fix, reusable extension, compatibility adaptation and Arcane-only behavior. For additions/removals/reverts use [history](../../rules/change-history-analysis.md); a title or marker is not proof of intent.

Choose placement through [trajectory](../../rules/fork-trajectory-priority.md) and [architecture](../../rules/architecture-and-ownership.md). Reuse supported owner-local extensions; keep necessary inherited edits minimal, preserve surrounding style and apply [marker rules](../../rules/arcane-edit-markers.md) only to our adaptations.

For actual conflicts follow [semantic merge policy](../../rules/merge-conflict-resolution.md): preserve agreed intent, accept compatible improvements, and remove an old Arcane implementation only on proved full current equivalence. Behavior decisions and sprite/map conflicts go to the user. Do not use contradictory marked/unmarked side-selection shortcuts.

Separate mechanical integration from behavior changes and do not copy a whole inherited file for one branch. Verify the affected consumers/resources under [verification](../../rules/verification.md), recording the reason for each intentional remaining divergence. Git history strategy remains separately authorized.
