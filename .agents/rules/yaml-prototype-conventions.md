
How prototypes read in this repository. For schema mechanics and validation commands see `.agents/skills/yaml-and-schema/SKILL.md` and `.agents/skills/prototypes/SKILL.md`.

## Key order

Declare the scalar header in this order:

```
type > abstract > parent > id > categories > name > suffix > description > components
```

Everything else follows the shape the type declares.

## Lists

New components are not indented relative to `components:`:

```yaml
# Correct
components:
- type: Sprite
  state: icon
```

```yaml
# Wrong
components:
  - type: Sprite
    state: icon
```

The same rule applies to every list and mapping value in the file. `tags:` values line up with `tags:`, not past it.

Use inline lists for `parent`, and regular lists for everything else. This is not a small-sample observation: inline `parent: [...]` appears in over a thousand entity prototypes in this repository.

```yaml
parent: [PartHuman, BaseHead]
components:
- type: Tag
  tags:
  - Head
```

Group `- type:` entries with no blank line between them. Separate distinct prototypes with one blank line.

## Components list ordering

Generalized and engine components first, specific content components last:

```yaml
components:
- type: Sprite # engine
- type: Physics
- type: Anchorable # generalized content
- type: Emitter # feature-specific
```

This makes a prototype readable top-down: what every instance has, then what makes this one different.

## Entity prototypes

```yaml
- type: entity
  abstract: true # omit when not abstract
  parent: BaseItem
  id: ItemThing
  name: thing
  components:
  - type: Item
```

`abstract: true` declares intent for an inheriting parent. Do not set textures on an abstract prototype; children inherit.

## Text fields

`name:` and `description:` take no quotes unless the value contains punctuation that requires them. Player-visible text comes from localization, not from literal YAML strings, except where the surrounding family already does it.

## Quoting

Quote only when required: leading special characters, a value that would parse as another type, or an embedded quote. This is the case the upstream conventions call out explicitly, for `description: 'A label on the packaging reads, ''Wouldn't a slow death make a change?'''`.

## Naming

- IDs and component type names are `PascalCase`.
- Everything else, including prototype type names, is `camelCase`.
- `prefix.Something` is never an ID. Dot-separated IDs collide with parent lookup and break compatibility.
- Use `suffix` to distinguish prototypes in the spawn menu without renaming the entity:

```yaml
- type: entity
  id: MobBase
  suffix: living
```

`id` stays stable; the player sees the suffix appended.

## Modifying existing prototypes

Change the smallest block. Keep existing key order rather than reordering the file, and keep existing quoting even when you would have chosen differently. Reorder only when adding a block that has an established position.

Use a targeted validator for the changed prototype and directly referenced resources when the repository provides one. The general YAML linter is a repository-wide check; do not run it for a local prototype edit by default. Run it only when the user requests broad validation or the task specifically requires that workflow, following `.agents/rules/verification.md`:

```powershell
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
```
