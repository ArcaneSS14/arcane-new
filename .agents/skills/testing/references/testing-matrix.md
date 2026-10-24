# Initial check selection

| Changed contract | Starting check |
| --- | --- |
| Shared/server/client C# | Affected project build; focused scenario test where useful |
| Network/prediction | Affected delivery/replay path, not only a handler unit test |
| Prototype / FTL | Actual loader/resource or locale validation; references and diff |
| RSI / sprite state | Relevant RSI validation and consumer reference; visual check for changed appearance |
| XAML | Affected client build and changed layout/lifecycle check as applicable |
| Database schema | Existing-row upgrade and generated migration for both supported providers |
| Packaging | Affected target/output/artifact contract |
| Documentation/instructions | Diff, changed links/anchors/routes; run a changed helper separately |

These are starting points, not an exhaustive mandatory suite. Expand for a concrete risk under [verification](../../../rules/verification.md), including a full run when necessary and authorized by the current execution scope.
