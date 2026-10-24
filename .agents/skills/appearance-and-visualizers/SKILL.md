---
name: appearance-and-visualizers
description: Implement replicated appearance data, GenericVisualizer mappings, sprite layers, overlays, and client visual systems.
---

# appearance-and-visualizers

## Когда применять

Use for client-visible state derived from authoritative or allowed predicted gameplay state: replicated appearance values, `GenericVisualizer` mappings, sprite layers, client animations, overlays, or client visual systems.

## До изменения

Trace the authoritative component and system through mutation, appearance update, client consumer or visualizer, and the target resource/layer. Identify the default appearance, RSI path/state, stable layer key, prediction path, and shutdown/deletion owner before changing a key or visual.

## Правила и границы

- Appearance is client-visible presentation data, never a second authoritative gameplay model. Keep gameplay checks and protected state out of visual code.
- Shared appearance keys contain only the small values the client needs. Do not replicate hidden state, large objects, or server-only decisions through appearance.
- After authoritative or permitted predicted mutation, update appearance through the existing owner path. A visualizer consumes replicated or predicted presentation data and must converge to authoritative state.
- Verify an RSI state exists, use stable layer keys, and retain a viable default appearance. Do not create a layer for every update when an existing layer can be reused.
- Clean up overlays, child visuals, animations, and subscriptions at component shutdown or entity deletion so no client visual survives its owner.

### Minimal examples

Correct: update an existing appearance key after authoritative mutation. Prohibited: use appearance as gameplay authority. Stop: report a missing client visual owner or resource/layer contract.

## Выбор подхода

Use `GenericVisualizer` or the nearest existing equivalent for simple state-to-layer/property mappings. Use a dedicated client visual system only for transitions, overlays, shaders, spawned visuals, or cleanup that cannot be expressed through the simple mapping. Keep the authoritative component/system as the source of the decision in either case.

## Антипаттерны

- Do not use appearance as authority, send a hidden gameplay value solely to drive a visual, or bypass the owner update path.
- Do not recreate layers, overlays, or subscriptions on every state update.
- Do not leave a predicted animation, child visual, or overlay after re-entry, component removal, or deletion.

## Проверка

Check RSI path/state, stable layer key, default appearance, late join, component add/remove, deletion, predicted re-entry, and state convergence. Confirm cleanup for overlays, child visuals, animation, and subscriptions.

## References

No reference is required for ordinary appearance routing; read [Visual lifecycle](../sprite-overlays-and-shaders/references/visual-lifecycle.md) when the change owns overlays, child visuals, shaders, map transfer, or reconnect cleanup.
