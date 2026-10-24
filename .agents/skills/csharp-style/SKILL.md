---
name: csharp-style
description: Write repository-consistent C# only after proving symbol access, ownership, and compatibility.
---

# csharp-style

## Когда применять

Use before writing or reviewing C# that crosses symbols, assemblies, ECS/prototype contracts, or compatibility surfaces.

## До изменения

Find every non-local declaration and verify signature, modifier, namespace, project/assembly, caller context, project reference, nullable context, and owner conventions.

## Правила и границы

- Do not infer APIs from names, old forks, snippets, or autocomplete. A project reference does not bypass access; partial types cannot span assemblies, sealed types cannot be inherited, and extensions cannot access private state.
- If access is unavailable, stop and propose the smallest public/internal owner extension point. Do not use reflection, copy private logic, widen visibility solely to compile, alter engine, or create parallel infrastructure.
- Keep files focused, public APIs small/stable, data before methods, guard clauses, thin handlers, bounded nesting, and explicit domain names. Respect nullability; use `!` only beside a proven invariant.
- Follow the current owner conventions for C#, entity/component patterns, allocation, comments, and compatibility. Public methods/events, serialized fields, prototype IDs, CVars, and network payloads require consumer discovery before broad rename/change.
- Avoid LINQ/allocation in hot paths and mutable internal collections; preserve cleanup on early returns.

### Minimal examples

Correct: verify declaration, access, assembly, and reference before a non-local call. Prohibited: use reflection to bypass private state. Stop: report inaccessible access with the needed extension point.

## Выбор подхода

Use the current owner pattern and explicit stable API for cross-assembly behavior. Choose a clear field over newer syntax when serialization, attributes, or clarity require it; adopt only language features confirmed by the current project configuration when they clarify the actual design.

## Антипаттерны

- Do not bypass modifiers, merge partial assumptions across assemblies, or add a reference merely to reach internals.
- Do not rename compatibility identifiers without searching consumers/migration needs, or rewrite working code solely to demonstrate syntax.

## Проверка

Verify declarations/access/references before calling them. Build only the affected project and run focused owner coverage when code behavior changed; documentation-only work needs diff checks.

## References

- [Dependency injection](references/dependency-injection.md): Read this when selecting an injected system, dependency field name, or common namespace; verify the current declaration before use.
