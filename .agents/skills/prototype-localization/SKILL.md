---
name: prototype-localization
description: Maintain English prototype localization and add Russian localization only when explicitly requested.
---
# prototype-localization

## Когда применять

Use for player-visible prototype text; not text resolved directly by code/UI.

## До изменения

Trace prototype field to locale key to en-US, including parent inheritance, overrides and nearby family keys.
Search prototype IDs, current and alternative keys, and nearby family keys; use the existing English owner file.

## Правила и границы

English is structural source. Preserve key variables, attributes, selectors and semantic literals. Change ru-RU only on request; use natural Russian without THE wrappers.
When Russian is requested preserve matching paths and ordering.

### Source ledger

Claim: Prototype localization resolves through the English owner file, and Russian updates are explicitly requested companion work.
Source: Root `AGENTS.md` and `.agents/rules/content-and-localization.md`.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing localization policy, FTL ownership, or prototype-resource layout.

### Minimal examples

Correct: resolve prototype field to its existing English locale owner. Prohibited: create Russian entries without request. Stop: report unknown field/key inheritance contract.

## Выбор подхода

Keep prototype text in the owner locale contract; route direct presentation text to localization-in-code.

## Антипаттерны

Do not expose prototype IDs, break inherited overrides, or leave stale locale keys after a field/key change.

## Проверка

Check field/key/en-US chain, inherited text, stale references and final contract; run targeted validation only when available.
Do not require restore, Release build or global YAML lint for FTL-only work.

## References

- [Prototype localization checklist](references/prototype-locale-checklist.md): Read this when adding or changing player-visible prototype text, inherited prototype text, or prototype-driven UI labels.
