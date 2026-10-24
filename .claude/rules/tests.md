# Tests adapter

For test edits use [tests-authoring](../../.agents/skills/tests-authoring/SKILL.md); for check selection/execution use [testing](../../.agents/skills/testing/SKILL.md) and [verification](../../.agents/rules/verification.md). Use the actual owner and production path, preserving assertions, user work and [SPDX/Git limits](../../.agents/rules/git-safety.md). [Locale policy](../../.agents/rules/content-and-localization.md) determines whether Russian culture behavior is in scope.

For review assess reachable regression coverage and actual results; do not launch a test/edit workflow merely because this adapter is present. Keep full failure evidence outside the source tree and return the relevant primary failure.
