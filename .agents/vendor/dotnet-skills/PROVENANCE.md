# Microsoft .NET Skills snapshot

Source: https://github.com/dotnet/skills

Immutable upstream commit: `a6bf43ab61967c30919334986515163ab24d7635`

Imported: 2026-10-24

License: MIT; see [LICENSE](LICENSE).

## Imported skills

- `dotnet`: `csharp-refactoring`, `msbuild`
- `dotnet-data`: `optimizing-ef-core-queries`
- `dotnet-diag`: `analyzing-dotnet-performance`
- `dotnet-msbuild`: `binlog-failure-analysis`, `binlog-generation`

The snapshot contains only portable skill directories and their referenced files. It deliberately excludes plugin manifests, LSP declarations, MCP servers, Copilot agents, host-specific metadata, tests, examples, automation, and scripts not needed by an imported skill.

## Intentional exclusions

- `dotnet-advanced`: its skills target file-based scripts, P/Invoke, package publishing, or narrowly specialized vectorization; none is required for current Arcane workflows.
- `dotnet-diag` `microbenchmarking`: excluded because the pinned upstream skill contains a broken local reference; vendored upstream text must not be adapted in this PR.
- `dotnet-nuget`: its Central Package Management conversion skill is not applicable to the current package-management structure.
- `dotnet-test`: Arcane `testing` and `tests-authoring` remain the owner-aware test-routing guidance; importing its broad host- and framework-specific suite would duplicate them.
- `dotnet-upgrade`: Arcane is currently on .NET 10; framework-upgrade guidance is imported only with a concrete upgrade task.
- Unselected skills in selected plugins either duplicate more specific Arcane guidance or target unsupported platforms, host tooling, or scenarios outside current Arcane work.

## Precedence

Arcane `AGENTS.md`, `.agents/rules`, `.agents/CATALOG.md`, owner guides, CI, current declarations, and Arcane-owned skills take priority over this vendored material. These skills are supplementary only: they do not establish Arcane API availability, ownership, engine boundaries, edit-marker policy, or default verification scope.

## Manual update workflow

1. Select a new immutable upstream commit SHA; never import an unpinned `main` revision.
2. Recheck upstream provenance, license, notices, and the candidate plugin contents.
3. Diff only the imported plugin paths against this snapshot.
4. Re-evaluate conflicts, duplication, and host-specific dependencies against Arcane rules and skills.
5. Replace the vendored snapshot without editing upstream skill text.
6. Update the SHA, date, imported/excluded lists, and any needed catalog routes.
7. Validate links, scope, attribution, and the final diff.
8. Submit the update as a separate PR.
