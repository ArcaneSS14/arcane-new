
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
| Documentation/guidance | `git diff --check`, `git diff --stat`, and final diff; validate changed links or paths when applicable |

Follow root `AGENTS.md`. Start with the smallest check covering the changed contract. Broaden for a concrete uncovered risk, a required gate, or an explicit request, including a full check when narrower checks cannot cover that risk. The absence of a targeted command alone does not justify a global run; explain the coverage gap and respect explicit task exclusions.
