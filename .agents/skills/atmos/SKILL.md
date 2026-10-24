---
name: atmos
description: Modify atmos simulation, gases, fire, pressure, pipes, devices, UI, and hot paths safely.
---

Atmos is authoritative simulation and often performance-sensitive.

## Ownership

Locate the existing simulation owner. Keep gas, fire, pressure, pipe networks, and device behavior on the server. Shared contains genuinely common gameplay logic/state and contracts where both sides need them; protected simulation stays server-owned. Client should render overlays and UI from replicated or explicit state.

## Data and units

Use existing units, gas identifiers, `GasMixture` patterns, device components, and alarm flows. Do not create parallel gas containers or convert units ad hoc. Keep device tuning prototype-driven when content authors need control.

## Performance

Avoid allocations, LINQ, repeated global queries, and broad per-tile work in update loops. Reuse buffers and existing batching. Review how often a device or tile update runs before adding work.

## UI and resources

Keep server simulation separate from BUI and XAML. Validate every UI request on the server. Keep the prototypes, UI, locale, overlays and sprite states affected by the behavior consistent; do not add unrelated surfaces.

## Verification

Select only cases exercising the changed contract under [verification](../../rules/verification.md); the following list is conditional, not a required full suite.

Test pressure boundaries, disconnected networks, deletion, anchoring, power loss, multiple gases, fire interaction, and malformed device settings when relevant. Run focused build/tests and YAML validation for touched prototypes.
