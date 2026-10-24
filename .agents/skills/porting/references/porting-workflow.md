
Create a migration inventory before editing:

- source PR family and history timeline, including introduction, follow-up fixes, reverts, and later restorations;
- explicitly requested source commit and any separately approved follow-ups;
- feature-owned files added, changed, renamed, or removed across that timeline;
- base/inherited files modified for integration;
- assets and licenses;
- dependencies;
- tests and maps;
- behavior added by each approved follow-up fix, plus risks found in unapproved later history.

For feature-owned files, use the requested source state as design evidence. For inherited files, re-derive the smallest destination change from current code.

Keep the destination diff logically separable by Shared, Server, Client, Resources, tests, and integration. This does not authorize multiple commits; commit count remains the user's decision.
