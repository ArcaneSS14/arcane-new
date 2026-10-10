
| Change | Minimum meaningful verification |
| --- | --- |
| Shared/server/client C# | affected project build; focused test when it covers the changed behavior |
| Network or prediction | focused test of the affected network/prediction path when available |
| Prototype | targeted schema/resource validation when available; build only if code also changed |
| FTL | targeted localization validation when available; no build by default |
| RSI or sprite state | targeted RSI validator and reference check when available |
| XAML | affected client project build; runtime layout check when available and relevant |
| Database schema | focused migration/upgrade check for the changed provider and path |
| Packaging | `Content.Packaging` build and requested package path |
| Documentation/guidance | `git diff --check`; validate changed links or paths when applicable |

Follow root `AGENTS.md`. Increase scope only when the changed behavior crosses multiple rows or the user requests broader validation. Do not run a global linter or full build when no targeted check exists unless broader validation was requested.
