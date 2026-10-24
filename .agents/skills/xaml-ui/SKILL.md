---
name: xaml-ui
description: Build localized client XAML controls with correct lifecycle, culture refresh, resource references, ownership, and validation.
---

# Client view structure

Keep static layout/styles in XAML; construct controls in code for genuinely dynamic or reusable content. Use existing containers/styles before manual positioning. Name only controls code-behind accesses, and verify generated names/types, constructors, resource URIs and the owning project's current XamlIL setup.

Code-behind owns view state and typed input events, not a second authoritative model. [BUI](../bound-user-interface/SKILL.md) or [EUI](../eui/SKILL.md) owns entity/session binding and transport; server validation is not replaced by button enablement.

Subscribe once and release longer-lived publishers on close/dispose. Prevent duplicate subscriptions after recreate/rebind, and cancel or reject deferred results for a window that no longer owns them. Reusable gameplay behavior stays in its system.

Text/culture refresh belongs to [localization-in-code](../localization-in-code/SKILL.md). Check constrained long strings for the locale/layout actually changed. Select affected client build, layout or lifecycle checks under [verification](../../rules/verification.md); a build does not prove the displayed layout.
