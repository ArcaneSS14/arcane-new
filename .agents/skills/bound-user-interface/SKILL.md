---
name: bound-user-interface
description: Implement BUI contracts, server validation, localized client windows, lifecycle cleanup, and state refresh.
---

# Entity-owned UI

Trace open request → server/framework opening checks → minimal state → client view → intent message → current authoritative validation/mutation → refreshed state or close. Verify the framework's actual actor, range and lifecycle guarantees and enforce remaining gameplay checks; disabled buttons or stale client snapshots are not authorization.

Shared owns key and serializable intent/state, Server protected decisions and data selection, Client BUI window creation/binding/transport, and [XAML](../xaml-ui/SKILL.md) controls/view state. Send typed values the viewer may know, not hidden data or a claimed client result. Avoid a second authoritative or stale state copy in the adapter.

Subscribe once per window lifetime and release references on close. Account for owner deletion, range loss, multiple viewers, stale state and reopen when affected. Keep per-viewer selection/filter state distinct from entity state. Cancel or reject callbacks for a closed/rebound owner/session; use the framework's identity mechanism.

Presentation text/culture belongs to [localization-in-code](../localization-in-code/SKILL.md). Choose checks for the changed message or lifecycle path under [verification](../../rules/verification.md); a presentation-only edit does not require every network case.
