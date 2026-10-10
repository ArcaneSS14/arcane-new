# Trauma module guide

Trauma is an inherited gameplay module in the TraumaStation sync trajectory. Keep Arcane-only features in `Content.Arcane.*`; changes to Trauma-owned paths follow the repository's Arcane edit-marker and fork-trajectory rules. Runtime projects remain at the repository root.

## Mechanics present

- Heretic and Wizard gameplay, including abilities, rituals, spells, and related UI.
- Cosmic Cult, Blood Cult, vampires, xenomorphs, zombies, and other antagonist or role content.
- Genetics and mutations, knowledge progression, martial arts, ranching, and language.
- Nuclear reactor/turbine, fire control, phones, salvage, station events, and gameplay extensions spanning shared/server/client layers.
- Common contains cross-layer events, configuration variables, and contracts; Shared contains most components and reusable behavior; Server owns authoritative rules and outcomes; Client owns visuals, UI, and input presentation.

The project-specific guides under this directory map actual source directories and example files by layer. Consult [`CATALOG.md`](../CATALOG.md) for project references and build impact. Resource and prototype coverage lives under root `Resources` and is not inferred from C# folders.
