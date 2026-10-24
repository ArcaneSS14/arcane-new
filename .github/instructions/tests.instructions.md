---
applyTo: "Content.Tests/**/*.cs,Content.IntegrationTests/**/*.cs"
---

<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>
SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

Follow [AGENTS](../../AGENTS.md). For test edits use [tests-authoring](../../.agents/skills/tests-authoring/SKILL.md); for selecting/running checks use [testing](../../.agents/skills/testing/SKILL.md) and canonical [verification](../../.agents/rules/verification.md). Reuse the actual owner and production entry. In review assess whether assertions cover the reachable regression; absence of a new test file alone is not a defect. Preserve user assertions and [SPDX/Git boundaries](../../.agents/rules/git-safety.md); locale scope follows [content policy](../../.agents/rules/content-and-localization.md). A review task does not authorize test execution or edits.
