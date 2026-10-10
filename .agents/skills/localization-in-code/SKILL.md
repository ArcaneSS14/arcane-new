---
name: localization-in-code
description: Resolve localized text at presentation boundaries using English-source keys, with Russian localization only when requested, and correct lifecycle refresh.
---

Keep internal logic typed and language-independent. Resolve localized text only where presented.

Before adding `LocId`, `Loc.GetString`, XAML localization, popup text, or validation text:

1. search the exact English key and competing spellings
2. inspect neighboring English keys used by the same feature
3. locate the existing English owner file; locate Russian only when it is in scope
4. preserve their relative path and message order
5. add or update Russian only when explicitly requested, preserving the English key contract

English defines message IDs, required attributes, variables, selectors, and ordering. When Russian is requested, preserve that contract while using natural language.

Do not store resolved localized strings in components, persistence, network state, equality checks, or durable caches.

If UI survives culture changes, refresh labels, tooltips, placeholders, and generated rows through the supported lifecycle. Subscribe once and unsubscribe on disposal or shutdown.

Pass typed values, entities, counts, durations, and prototype-derived text as variables. Variable names and selector input types must match English; they must also match Russian when Russian is in scope.

Use entity-name and grammar helpers instead of manual name, pronoun, or article concatenation. When Russian is in scope, do not use `THE(...)` or equivalent English grammar wrappers.

Review failures include hardcoded player text, mismatched variables, localized string comparison, persisted output, stale UI, and duplicate culture-change subscriptions. Russian key coverage and ordering are review criteria only when Russian is in scope.

Inspect the affected FTL file and exact keys, then review the code or XAML that resolves them. Search plausible owner roots for an exact duplicate key only when needed. Follow root `AGENTS.md` for verification scope; FTL-only changes do not require dependency restore, a full build, or YAML linting by default.
