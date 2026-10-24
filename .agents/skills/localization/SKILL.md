---
name: localization
description: Maintain English-source localization, adding Russian only when requested, with correct owner paths, variables, selectors, and runtime behavior.
---

# FTL and prototype text

[Content/localization policy](../../rules/content-and-localization.md) owns locale selection, English structure and Russian exceptions. This skill applies it to the affected key/consumer; it does not trigger a translation or full locale inventory.

Find the existing feature's en-US owner file and exact/old/proposed keys, then its real code/XAML/prototype/map consumers as relevant. Preserve casing, underscores, grouping and paths; do not derive a directory from a module name. Check competing keys/load roots only for an actual ownership or duplicate question.

Preserve attributes, variables, selector input types, markup, line breaks and control hints. Search consumers before intentional rename/removal and stale variants afterward. Do not silently invent an uncertain translation; record the domain question when it matters.

## Prototype-generated text

Inspect the prototype type's conventions and inherited text from abstract parents. Check the changed name/description, suffix/category, marking, construction/machine, dataset, examine or generated UI label as relevant. Search both prototype ID and locale key before declaring text unused; verify fallback and entity grammar at the consumer. A machine ID is not a player display label.

Inspect changed FTL and direct references; use an existing targeted validator under [verification](../../rules/verification.md). Do not require Release resource validation merely because a key was added. Runtime resolution/culture lifetime belongs to [localization-in-code](../localization-in-code/SKILL.md).
