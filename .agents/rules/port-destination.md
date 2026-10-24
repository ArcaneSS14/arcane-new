# Port destination

Choose provenance and owner before copying implementation. The default for a port/cherry-pick is root Content.Arcane.Common/Shared/Server/Client and the existing Resources/**/_Arcane/** roots. Modules/Arcane contains guides, not runtime code. Adapt behavior to the destination architecture instead of mechanically copying files.

## Provenance exceptions

- A verified TraumaStation addition belongs in the corresponding Trauma-owned or vanilla path so later sync can carry it forward. Establish this from the actual source commit/diff, not a title, namespace or Trauma marker.
- Restoration of content removed by a Trauma revert/deletion belongs in Arcane-owned paths, even if delivered as a new file and originally Trauma-owned. This exception takes precedence over the Trauma-addition exception. Trace the introduction, removal and fixes with [history analysis](change-history-analysis.md).
- Necessary integration in inherited paths remains minimal and [marked](arcane-edit-markers.md). Arcane-specific adaptations use Arcane ownership where supported. If history cannot establish the Trauma exception, default to Arcane and report the uncertainty.

Use [architecture](architecture-and-ownership.md) for layer/dependency direction and the actual guide from [Modules/CATALOG.md](../../Modules/CATALOG.md). Common must not depend on Arcane Shared/Server/Client; do not widen references just to compile a port. Shared contracts/logic, server authority and client presentation retain their responsibilities.

## Selected behavior and resources

Search the likely destination for an existing equivalent. A pinned historical source_ref remains the task's source; report material later fixes/risks separately and clarify any behavior change. When current source behavior is requested, include its accepted follow-up fixes. Adapt obsolete calls; do not deliberately reproduce a known defect.

Use [third-party-materials](third-party-materials.md) for source/ref/license/attribution and [content-and-localization](content-and-localization.md) for locale scope. Check exact existing asset paths and equivalents before adding a new RSI or resource hierarchy. Preserve source authorship/notices where required; do not present imported untouched lines as Arcane-authored adaptations.

## Evidence and acceptance

Keep a compact source-family [manifest](../skills/porting/references/source-pr-family-manifest.md): source revision, relevant fixes/reverts, intended behavior, included/excluded parts and rights. Report destination paths, necessary inherited edits and checks/omissions.

Select checks for actual changed owners/contracts under [verification](verification.md). Do not create test infrastructure merely because this is a port. Confirm justified destinations and correct marker boundaries; minimal inherited integration is allowed and must not be rejected by an absolute no-base-files checklist.
