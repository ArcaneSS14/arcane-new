
Base content lives in root `Content.Common`, `Content.Shared`, `Content.Server`, `Content.Client`, and `Resources`.

Arcane projects also live at the repository root: `Content.Arcane.Common`, `Content.Arcane.Shared`, `Content.Arcane.Server`, and `Content.Arcane.Client`. Arcane-owned resources use `_Arcane` directories under the existing `Resources` tree.

The solution also contains root-level Goobstation, Lavaland, Factory, Medical, and Trauma project families. Do not infer that a directory under `Modules/` is the runtime root; in this checkout those directories hold scoped guidance. Confirm the project and resource owner from the current solution and project files.

Verification projects include `Content.Tests`, `Content.IntegrationTests`, `Content.YAMLLinter`, and `Content.Packaging`.

`RobustToolbox/` is the engine boundary.

Old `Content.* /_Orion` and `Resources/**/_Orion` paths from `Orion-Station-14` are migration source evidence only.
