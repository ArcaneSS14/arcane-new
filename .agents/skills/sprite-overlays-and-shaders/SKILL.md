---
name: sprite-overlays-and-shaders
description: Implement sprite layers, overlays, render targets, shader prototypes, and visual cleanup without leaking gameplay authority.
---

## Когда применять

Use for client sprite layers, entity-local or global overlays, shader parameters, render targets, or presentation effects derived from replicated or permitted predicted state.

## До изменения

Identify the owner and cleanup lifecycle for each sprite layer, overlay, shader parameter, and render target. Trace component shutdown, state exit, map transfer, disconnect, deletion, and resolution/viewport changes before allocating a visual resource.

## Правила и границы

- Use stable sprite layer keys and reuse existing layers. Verify RSI path/state, visibility, color, scale, rotation, animation, and resource fallback.
- Give every overlay, child visual, shader parameter, and render target a clear owner and cleanup path. Remove entity-local resources on component shutdown, state exit, map transfer, disconnect, or deletion as applicable.
- Update expensive shader parameters only when their value changes. Treat render targets and viewport-dependent resources as lifecycle-sensitive across resolution or viewport changes.
- Use entity-local overlays for entity-local effects; reserve global overlays for effects genuinely shared by the viewport or client. Do not use a global overlay merely to display an entity-local effect.
- Visual code consumes allowed presentation data only. It does not receive hidden gameplay state or perform authoritative mutation.

## Выбор подхода

Prefer a sprite layer for a persistent entity-owned visual, an entity-local overlay for transient entity presentation, and a global overlay only for a viewport-wide effect. Use a shader or render target only when the established client rendering path needs it; otherwise reuse the nearest existing sprite/overlay pattern.

## Антипаттерны

- Do not recreate a layer or overlay per update, drive an entity-local effect through a global overlay, or retain a visual after its owner disappears.
- Do not update a costly shader parameter every frame without a changed input.
- Do not move authority, hidden state, or protected gameplay checks into visual code.

## Проверка

Check RSI state/path, visibility, color, scale, rotation, animation, and missing-resource fallback. Cover component add/remove, state exit, late join, reconnect, deletion, map transfer, resolution/viewport changes, and multiple simultaneous effects.

## References

- [Visual lifecycle](references/visual-lifecycle.md): Read this when adding or changing overlays, child visuals, shaders, client visual cleanup, map transfer, or reconnect behavior.
