# Commenting conventions

Where comments belong, and where they do not. The distribution in this repository is not even: components carry dense documentation, systems carry almost none.

## Where comments are expected

### Components and prototype types

Every `[DataField]` member gets an XML doc comment. So do prototype types and the configuration records they expose.

This is not stylistic preference. A `[DataField]` member is populated from YAML by name, so its field name and its doc comment are the only documentation a designer or content contributor ever sees. `Content.Shared/Polymorph/PolymorphPrototype.cs` has 26 `<summary>` blocks across 53 members. That density is the local norm: 1027 of 1061 component files carry comments, at a median of one comment line per line of code.

```csharp
/// <summary>
/// What entity the polymorph will turn the target into.
/// Must be in here because it makes no sense if it isn't.
/// </summary>
[DataField]
public EntProtoId? Entity;
```

For a field whose constraint is not obvious from its type or name, say the constraint:

```csharp
/// <summary>
/// Delay between polymorph uses in seconds.
/// </summary>
[DataField(serverOnly: true)]
public int Delay = 60;
```

### Complex logic

Comment where a reader would otherwise have to reconstruct the reasoning:

- a non-obvious ordering constraint, and why it exists
- a deliberate bound or early return, and what breaks without it
- a workaround for framework behavior, with the behavior named
- physics or math derivations, with the equations in the comment
- an invariant that a future edit could silently break

From `Content.Trauma.Shared/Containers/ContentContainerSystem.cs`, which shows both the expected and the unwanted shape:

```csharp
// Prevent players from being sent to null-space when carried by a polymorphing entity
var stack = new Stack<EntityUid>();
...
if (stack.Count < 1000) // Unlikely to have over 1000 nested entities unless there is an infinite loop.
    stack.Push(entity);
```

The first explains why the traversal exists. The second documents a bound and the condition that would violate it. Both are load-bearing.

Comments in systems do concentrate on reasoning. Representative examples from the current tree:

```csharp
// this is the actual entity-world targeting magic
// Server-validated, AnyCommand is acceptable here.
// This, too, is O(n^2). I think.
// It cannot be null if its handled, but good to check to avoid ugly null ignores
```

Each names a framework subtlety, a security constraint, a complexity claim, or the reason a null check exists. That is the acceptable register, not narration.

## Where comments are not expected

### Narrating mechanics

A comment restating the code is noise. Delete it.

```csharp
// Increment the counter
_count++;

// Add the entity to the list
list.Add(entity);
```

Nothing is gained. The code already says this, and the comment can only fall out of sync.

### Restating a signature, condition, or type

```csharp
// Loop over all entities
foreach (var ent in query)

// Returns true if the entity has the component
public bool Has(EntityUid uid)
```

Prefer XML docs on public and `[DataField]` members over a comment above them.

### Section banners

Do not add `// --- Dependencies ---`, `// --- Helpers ---`, or `// Region X`. Member order already communicates grouping: fields and auto-properties before methods.

### Marking ownership

Do not annotate a file or block with the contributor, fork, or migration it came from. Ownership lives in SPDX headers and the repository's identity rules.

## Handling existing comments

When editing a line, remove a comment that only narrates it. Do not launch a comment-removal pass over a file as a side effect of an unrelated change.

Match the density of the file you are in. Adding a `// this increments the counter` to a well-documented file is as wrong as deleting a genuine derivation comment from one that needs it.

## License headers

The SPDX header at the top of a file is not a comment in this sense. Never add, remove, reorder, or normalize one without the user explicitly requesting that exact SPDX change. See `AGENTS.md`.
