---
name: porting
description: Port one complete feature family with provenance, current-API adaptation, repository ownership, localization, resources, and verification.
---

# Port a defined feature family

Establish the source repository and exact source_ref, intended behavior, destination and exclusions. A specified historical revision remains the source contract; inspect material later changes as separate risks. For a current-behavior request include accepted follow-up fixes. Do not silently widen the port or reproduce known broken behavior.

Trace the relevant introduction/fixes/reverts/restorations with [history analysis](../../rules/change-history-analysis.md). Keep a compact [source-family manifest](references/source-pr-family-manifest.md) for actual changed layers, resources, dependencies, omissions and rights; do not inventory unrelated subsystems.

Check [third-party rights](../../rules/third-party-materials.md) before importing. Choose [destination](../../rules/port-destination.md), including its Trauma-addition/restoration exceptions, and [architecture](../../rules/architecture-and-ownership.md). Re-derive minimal inherited integration from current destination APIs; preserve required notices and distinguish imported unchanged authorship from our adaptations under [markers](../../rules/arcane-edit-markers.md).

Map only affected contracts: authority, units/cadence, lifecycle, transport, UI, persistence, prototypes/assets and [locales](../../rules/content-and-localization.md). Verify actual declarations, loader and consumers rather than matching names. Keep independent feature families separate; logical diff organization does not authorize extra commits.

Select checks for changed owners and contracts under [verification](../../rules/verification.md). Report source revision, meaningful exclusions, necessary inherited edits, compatibility risks and actual results/omissions. Public contribution does not require a private skills library.
