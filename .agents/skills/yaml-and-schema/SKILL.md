---
name: yaml-and-schema
description: Author and diagnose YAML, prototype schemas, workflows, manifests, and structured configuration with exact validation.
---

# Structured YAML by consumer

Identify the loader first: prototype manager, map serializer, GitHub Actions, actual manifest/config loader or skill frontmatter. YAML syntax alone does not establish schema/behavior validity.

Use a current nearby destination structure. Check scalar types, list/map shape, indentation/quoting, duplicate keys, anchors, required fields and exact referenced IDs/paths. Frontmatter must begin at the document start; workflow expression quoting and map/prototype schemas have different consumers.

Verify changed component fields/enum names against declarations rather than copying source-fork names. Avoid unrelated formatting and mass map serialization. Route prototype/map/resource contracts to their corresponding skill.

Use the actual domain validator/workflow for affected files under [verification](../../rules/verification.md). Do not require a generic build for every YAML file or duplicate the same specialized check. Build executable projects only when the changed contract needs them; record the limitation when no targeted validator exists.
