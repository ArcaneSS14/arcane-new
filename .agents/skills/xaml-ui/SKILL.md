---
name: xaml-ui
description: Build localized client XAML controls with correct lifecycle, culture refresh, resource references, ownership, and validation.
---

## Когда применять

Use for client XAML controls, BUI/EUI windows, and their presentation code-behind. Gameplay authority, server services, and authoritative validation are outside this skill.

## До изменения

Find the owning BUI/EUI flow, existing XAML layout, generated named controls, code-behind, resource URI, subscriptions, and window close/deletion path. Verify generated names, control types, constructors, and lifecycle APIs in current code before using them.

## Правила и границы

Keep static layout, labels, and visual hierarchy in XAML; keep presentation state, wiring, and disposal in code-behind. Create named controls only for elements code-behind actually accesses, and prefer existing containers, styles, and UI patterns over manual positioning. Generate controls in code only for genuinely dynamic content or reusable custom controls. Use current generated names and resource conventions. A window must not call server services, decide authority, or authorize an action. Cancel or release subscriptions, timers, and callbacks on close/dispose and never update a closed window. Resolve localized text at presentation and refresh culture through the existing lifecycle; add Russian only when explicitly requested.

## Выбор подхода

Choose XAML for static structure and themes/resources, code-behind for narrow state-to-control adaptation, a BUI/EUI contract for data and intent messages, and the gameplay owner for mutation.

## Антипаттерны

Do not duplicate gameplay logic or network validation in a window, create parallel named controls, or leave an event handler active after disposal.

## Проверка

Check creation, state update, close/reopen, disposal, delayed callbacks, resource URI, culture refresh, and absence of server-only data in the presentation layer. When `ru-RU` is explicitly in scope, also check long translated strings.

## References

- [XAML structure](references/xaml-structure.md): Read this when designing or reviewing XAML layout, named controls, code-behind ownership, event subscriptions, or resource URIs.
- [Testing matrix](../testing/references/testing-matrix.md): Read this when selecting XAML or client UI verification.
