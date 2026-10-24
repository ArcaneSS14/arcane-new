---
name: localization
description: Maintain English-source localization, adding Russian only when requested.
---
## Когда применять
Use for player-visible FTL keys and locale contracts.
## До изменения
Find owner locale path, exact key, callers and alternatives; for rename/delete search old, new and related keys with consumers.
Confirm the owner path so a new file cannot shadow an existing locale file. For English-only work do not search or edit ru-RU.
## Правила и границы

### Source ledger

Claim: `en-US` is the structural localization source and `ru-RU` changes require explicit request.
Source: Root `AGENTS.md` and `.agents/rules/content-and-localization.md`.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing repository localization policy or locale-root ownership.
en-US is structural truth. Do not edit ru-RU without an explicit request. Preserve attributes, variables, selectors, markup, placeholders and ordering.
Preserve line breaks and control hints. When Russian is requested use natural Russian; do not retain English placeholders or THE wrappers.
### Minimal examples

Correct: change the existing English owner file for visible text. Prohibited: update Russian for an English-only task. Stop: report unknown key owner or locale contract.

## Выбор подхода
Use localization-in-code when code/UI resolves text and prototype-localization when a prototype field resolves a key. Never use a prototype ID as display fallback.
## Антипаттерны
Do not invent uncertain translations, flatten selectors, or leave stale key consumers.
## Проверка
Inspect affected en-US structure and final FTL diff; run a targeted validator only when it exists.
Do not run restore, a full build or global YAML lint for FTL-only work.
## References
- [Localization workflow](references/localization-workflow.md): Read this when adding, moving, renaming, or reviewing player-visible locale keys and their usage.
