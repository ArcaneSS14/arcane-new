<!--
SPDX-FileCopyrightText: 2026 PuroSlavKing <103608145+PuroSlavKing@users.noreply.github.com>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

# Arcane Shared guidance

Shared contains Arcane components, shared systems, network contracts, BUI messages and states, and prediction-compatible behavior.

Assume shared logic may run on both client and server and may run repeatedly during prediction. Keep hidden state and protected decisions on the server. Use networked fields only for information the client genuinely needs, and dirty authoritative mutations.

Do not reference Arcane Server or Client. Prefer reusable shared APIs over duplicated side-specific logic.
