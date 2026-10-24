---
name: prototypes
description: Create, inherit, compose, localize, and validate YAML prototypes in the correct resource root.
---

# Effective prototype data

Find a current nearby prototype of the same type and real resource owner. Use the actual loader/project metadata; inspect a module.yml only if that owner has one. Placement/markers follow [trajectory](../../rules/fork-trajectory-priority.md), not a generic underscore exemption.

Verify fields against current component/serializer schema. Prefer supported inheritance/composition to copying a large parent; check effective defaults, parent chain, abstract/spawnable intent, component merge/replacement and permitted remove/clear syntax.

Use stable IDs and typed code IDs. Index represents required data; TryIndex fits expected absence. Verify directly used prototype/RSI state/sound/dataset/map/key references, duplicate IDs and stale source-fork fields. Do not run a global resource inventory for a local change.

Visible/inherited prototype text follows [localization](../localization/SKILL.md), including its prototype subsection. Select loader/reference validation or focused lifecycle/map-loading tests for the changed risk under [verification](../../rules/verification.md); no automatic Release build or complete integration suite.
