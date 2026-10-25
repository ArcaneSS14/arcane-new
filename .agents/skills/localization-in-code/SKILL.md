---
name: localization-in-code
description: Resolve localized text at presentation boundaries using English-source keys and safe typed variables.
---

# localization-in-code

## Когда применять

Use when presenting player-visible UI, popup, chat, or log text; do not use localization as gameplay-state storage.

## До изменения

Search the exact FTL key, competing spellings, and neighboring keys. Find the owner FTL file, callers, presentation boundary, typed variables, entity/count/grammar context, and existing culture-refresh lifecycle. Preserve the owner file's relative path and message order.

## Правила и границы

Resolve localization only at the presentation boundary. Do not store resolved text in a component, persistence, network message, equality key, or long-lived cache. Carry the key and typed variables to display; pass entities, counts, and grammar through existing typed helpers rather than concatenated text. Variable names and selector input types must match the English contract. Use entity-name and grammar helpers instead of manually joining names, articles, or pronouns. Follow the owner key path and resolve only where the player sees the result. Subscribe once to culture changes and unsubscribe on disposal or shutdown. `en-US` is structural source; change `ru-RU` only when explicitly requested, and never use `THE(...)` in Russian text.

### Source ledger

Claim: English FTL structure is the repository contract, while Russian FTL changes are opt-in and preserve the English key/variable contract.
Source: Root `AGENTS.md` and `.agents/rules/content-and-localization.md`.
Source type: Arcane
Checked against branch: `66cf1fb0a7`
Volatility: branch-bound
Recheck trigger: Changing localization policy, locale roots, or FTL ownership rules.

### Minimal examples

Correct: pass an FTL key and typed variables to presentation. Prohibited: store resolved text in network state. Stop: report an unverified key/consumer/grammar helper.

## Выбор подхода

Use an FTL key for player-visible text, typed variables for changing data, and owner-local code to choose the key. Send a server-only fact to UI only when that specific viewer is allowed to see it.

## Антипаттерны

Do not send resolved localized text over the network, compare translated strings, cache them across a culture change, or create a new FTL family without locating the existing owner.

## Проверка

Inspect the affected FTL key and its consumer directly. Check variable/entity/count cases, grammar helpers, culture refresh, absence of resolved text in state/contracts, and safe viewer-specific output. FTL-only work needs targeted FTL/code inspection, not restore, a full build, or YAML linting.

## References

- [Entity text and popups](references/entity-text-and-popups.md): Read this when composing entity-aware text, popup feedback, predicted feedback, or messages that may reveal server-only information.
