---
name: atmos
description: Modify atmos simulation, gases, fire, pressure, pipes, devices, UI, and hot paths safely.
---

## Когда применять

Use for authoritative atmosphere simulation: gases, pressure, fire, pipe networks, atmospheric devices, alarms, and presentation/UI derived from their state.

## До изменения

Find the owner system, gas/pressure/fire/pipe/device contract, update frequency, prototype tuning, and power, anchoring, and network dependencies. Trace which state remains on the authoritative server and which minimal presentation/UI state reaches the client.

## Правила и границы

- Atmos is authoritative server simulation. Shared contains only required components, enums, contracts, and UI messages; Client receives presentation or UI state only.
- Use existing units, gas identifiers, `GasMixture` patterns, device components, and alarm flows. Do not create parallel gas containers or perform ad-hoc unit conversion.
- Make author-configurable device tuning prototype-backed. Keep simulation decisions and protected device state on the server.
- Avoid LINQ, allocations, repeated global scans, broad per-tile queries, and unnecessary work in hot update paths. Reuse existing buffers/batching and establish update frequency before adding work.
- Keep BUI/XAML separate from simulation. Validate every UI request on the server and send minimal state; do not make UI an authority path.
- When one feature changes prototype tuning, UI, player-visible text, overlays, or sprite states, trace and update each affected owner surface together; add Russian only when explicitly requested.

### Minimal examples

Correct: extend the existing server gas/device owner with its current units. Prohibited: create a parallel gas container. Stop: report when the gas/device API or simulation owner is unverified.

## Выбор подхода

Extend the existing gas, pipe, device, or alarm owner rather than creating a parallel container or update loop. Use prototype-backed fields for content tuning and component/system state for runtime simulation. Add client overlays or UI only as presentation consumers of authoritative state.

## Антипаттерны

- Do not let client input decide pressure, gas, fire, or device outcomes.
- Do not invent units or gas identifiers, duplicate an existing device/alarm flow, or hide simulation work in a UI callback.
- Do not add an allocation, LINQ query, or broad scan to a repeated tile/device update without proving its bounded frequency.

## Проверка

When affected, check pressure limits, disconnected networks, power loss, deletion, anchoring, multiple gases, fire interaction, and malformed device settings. Use focused owner validation only for touched prototypes or runtime behavior; do not restore or run a full build for skill-only changes.

## References

No reference is required for ordinary atmos routing; inspect the existing gas, pipe, device, or alarm owner before changing its simulation flow.
