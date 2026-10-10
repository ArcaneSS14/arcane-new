
Create a migration inventory before editing:

- source PR family and history timeline, including introduction, follow-up fixes, reverts, and later restorations;
- final source commit;
- feature-owned files added, changed, renamed, or removed across that timeline;
- base/inherited files modified for integration;
- assets and licenses;
- dependencies;
- tests and maps;
- behavior added by every follow-up fix.

For feature-owned files, use the final state as design evidence. For inherited files, re-derive the smallest destination change from current code.

Commit the destination PR in logical layers such as Shared, Server, Client, Resources, tests, and integration.
