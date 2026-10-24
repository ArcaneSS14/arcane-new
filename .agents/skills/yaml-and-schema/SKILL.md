---
name: yaml-and-schema
description: Author and diagnose YAML, prototype schemas, workflows, manifests, and structured configuration with exact validation.
---

YAML syntax validity is only the first layer. Identify the actual consumer and schema.

## Workflow

1. Identify the loader: prototype manager, GitHub Actions, module loader, map serializer, localization tooling, or custom configuration.
2. Copy structure from a current nearby destination file.
3. Verify scalar types, indentation, list and map shape, quoting, anchors, and required fields.
4. Verify every ID, path, component field, and referenced resource.
5. Avoid formatting unrelated blocks or reserializing maps.
6. Run the domain-specific validator when one exists for the changed format.

## Common failures

- valid YAML with an unknown component field;
- duplicate map keys;
- string where a number or boolean is required;
- invalid workflow expression quoting;
- frontmatter not starting on the first line;
- module manifest path from another repository;
- prototype field renamed in destination code;
- locale or prototype file placed outside the owner root.

## Exact validation commands

Validate the changed YAML files with the narrowest available validator. For workflow, map, RSI, or custom schema files, use the exact specialized command from the current repository workflow. Build a code project only when code changed or the selected validator requires built binaries. Follow root `AGENTS.md`; do not restore dependencies, run a full build, or run a repository-wide linter by default. Do not guess validation commands from another fork.
