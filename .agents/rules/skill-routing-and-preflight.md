
Before editing:

1. Read root `AGENTS.md` and every scoped `AGENTS.md` that applies to the intended files.
2. Use `.agents/CATALOG.md` to select only the task-specific skills needed.
3. Inspect the current implementation and nearby patterns before proposing a new one.
4. Confirm paths, APIs, project references, and resource roots against the current branch.
5. Identify required verification before implementation so the design remains testable.

## Source precedence for skills

When a skill needs a technical claim, use sources in this order:

1. Current Arcane `AGENTS.md` files, `.agents/rules`, `.agents/CATALOG.md`, owner guides, CI, and declarations.
2. The pinned `RobustToolbox` and current-branch source for engine APIs and behavior.
3. Space Wizards documentation for general SS14 and RobustToolbox concepts.
4. Microsoft documentation for C#, .NET, async, exceptions, BCL, and EF Core.
5. Repository history and other forks only as context, never as the source of current APIs.

Arcane-local rules win when sources conflict. External documentation never replaces checking declarations in the current repository. Recheck branch-bound and version-bound claims against the current branch. New or substantially rewritten skills must link each applicable `references/` document from their `SKILL.md` with a specific trigger for when to read it; audit and routing of existing references is separate work.

Do not load every skill by default. Excess context can hide the instructions that actually matter. Load one routing skill such as `gameplay-feature`, then the narrow technical skills for the affected layers.

Refresh assumptions when the branch changes, a dependency is updated, or a source port targets a different revision. Never rely on a path or API remembered from `Orion-Station-14` without confirming it exists here.
