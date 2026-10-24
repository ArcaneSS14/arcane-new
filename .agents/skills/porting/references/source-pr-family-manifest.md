# Source-family record

Fill only relevant fields; these placeholders are not a particular repository or a completed port.

```yaml
feature: feature-name
source_repository: owner/repository
source_ref: selected-full-sha
destination_base: destination-full-sha
root_pr: null
included_prs: []
excluded_prs: {} # reason per meaningful exclusion
introducing_commits: []
follow_up_commits: []
reverted_commits: []
restoration_commits: []
later_history_inspected: [] # evidence/risks, not a silent change of source_ref
behavior_contract: "Selected intended behavior and evidence."
files: {added: [], modified: [], renamed: [], removed: []}
dependencies: []
rights: [] # source/ref/path, license and required attribution
destinations: []
necessary_inherited_edits: []
checks_and_gaps: []
unresolved_decisions: []
```

Maintain the record with [history analysis](../../../rules/change-history-analysis.md). A pinned source_ref and inspected later history have distinct purposes.
