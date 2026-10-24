---
name: build-and-packaging
description: Change projects, solution structure, CI, generated content, and packaging with repository-correct ownership and exact commands.
---

# Build graph and outputs

For a changed project/target/workflow identify its owner, actual inputs, consumers and produced output/artifact/package. Confirm relevant references, namespace/assembly name, solution grouping and resource loading. Inspect XamlIL/generated-code targets or module.yml only when that real owner uses them.

Follow [architecture](../../rules/architecture-and-ownership.md) for dependency direction and existing infrastructure. Reuse an existing project/CI target; test infrastructure is not a runtime manifest entry. Check package contents include required files exactly once and downstream consumers still locate them.

Use the current affected workflow's exact flags, artifact paths and configuration, not another fork's commands. CI gates may test DLLs or csproj; warning-property semantics and real test owners are in [verification](../../rules/verification.md). Do not duplicate those commands here. Preserve supported initialization/output dependencies when changing CI, but do not run submodule initialization without explicit [engine authorization](../../rules/engine-boundaries.md).

Build or exercise only the changed target/consumer contract first. Expand configurations/platforms when a concrete risk requires them. If a test fails on a missing native runtime, inspect the current CI runtime-link setup before blaming the source; do not add symlinks speculatively to every run.
