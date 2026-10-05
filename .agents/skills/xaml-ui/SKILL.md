---
name: xaml-ui
description: Build localized client XAML controls with correct lifecycle, culture refresh, resource references, ownership, and validation.
---

XAML is client presentation. Keep authority and protected validation out of code-behind.

1. Verify repository identity, owner-local paths, and edit-marker requirements.
2. Find a nearby control using the same UI framework.
3. Verify control types, generated names, constructors, and lifecycle APIs.
4. Keep code-behind focused on state binding and UI intent.
5. Localize every visible label, tooltip, placeholder, validation message, and generated row.
6. Treat English as structural truth and mirror Russian keys and order.
7. Test long Russian strings, scaling, disabled state, reopen, disposal, and culture refresh.

Subscribe once and unsubscribe on disposal or shutdown. Do not cache resolved labels when a window survives culture changes.

Mark inherited changes with the current repository marker. Do not add redundant markers in owner-local module or underscore paths.

Run the DebugOpt build, Release resource validation, and the existing integration-test project for every affected owner.
