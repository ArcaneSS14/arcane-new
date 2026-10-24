---
name: forms-and-input-validation
description: Validate UI, command, text, numeric, entity, and configuration input with localized feedback and server authority.
---

## Когда применять

Use for any text, number, entity selection, command, or configuration value received from a player, UI, or external input.

## До изменения

Determine the field format and owner, consumer, trust boundary, allowed bounds, localized feedback, and authoritative handler that actually applies the value.

## Правила и границы

Client validation provides early, clear feedback; the server repeats the exact authoritative validation. Before domain validation, define trimming/normalization, an explicit culture and numeric format, length, bounds, and overflow behavior. Reject ambiguous integer forms such as `1.0` unless the domain permits them, and check length before expensive work. Then validate ranges, overflow, whitelists, prototype IDs, entity existence and ownership, access, cooldowns, current state, and cross-field constraints. Player errors must be safe and localized: never expose internal exceptions, raw IDs, or server-only reasons. Treat markup, paths, URLs, commands, and identifiers as hostile input and use an appropriate allowlist, escaping, or parse policy.

## Выбор подхода

Separate parse failure, syntactic validity, and domain validity. Use one authoritative validation source so UI and server rules cannot drift. Use a typed message or contract when input crosses the network; transmit intent, not a client-claimed result.

## Антипаттерны

Do not rely on a disabled control, an implicit current culture, unchecked numeric conversion, or an error message that reveals protected state. Do not concatenate shell, SQL, markup, or resource paths; use structured APIs instead.

## Проверка

Keep validation-message variable names and values aligned with the `en-US` contract (and Russian only when it is in scope). Cover empty and whitespace input, bounds, overflow, invalid format, long input, hostile characters, stale entities, unauthorized actors, race/retry, and server rejection after client acceptance.

## References

- [Numeric and text input](references/numeric-and-text-input.md): Read this when defining numeric parsing, range, rounding, text normalization, length, character, or escaping policy.
