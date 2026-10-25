---
applyTo: "Content.Shared/**/*.cs,Content.*.Shared/**/*.cs"
---

<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

Review Shared code for prediction-safe contracts. Hidden authority remains server-side and client-only dependencies do not enter Shared; request network or replay evidence only for a changed replicated or predicted flow.
