
## Engine access requires an explicit user request

`RobustToolbox/` is off limits unless the user explicitly asks for a change inside the engine in their current request.

Without that explicit request, do not touch the engine in any way. This applies to:

- editing, creating, deleting, moving, or renaming any file inside `RobustToolbox/`;
- running any command whose path or working directory is inside the engine, including `git -C RobustToolbox ...`;
- running `git submodule update`, changing the submodule pointer, or staging engine paths;
- treating engine internals as an implementation option to reach otherwise inaccessible state.

Read-only validators that CI itself runs against content are permitted, because they read content and only report on it. The current example is `python3 RobustToolbox/Schemas/validate_rsis.py Resources/`. This exception covers running the validator, never modifying it.

An implicit need is not permission. Discovering that a solution would be easier in the engine, or that content cannot access something the engine owns, does not authorize an engine change.

When the content-side solution is blocked, implement the smallest legitimate content-side extension point, or STOP and report the exact declaration, its access modifier, the involved assemblies, and the engine boundary you would need to cross. Then wait for the user to decide. Propose the engine change in the report; do not begin it.

If the user requests an engine change in one message, that approval covers only that change. A later, separate task requires a new explicit request, and an unrelated task in the same session does not inherit it.

## Content work that needs engine knowledge

Reading the engine to confirm a signature is part of normal symbol verification and is not an engine change. Do not modify anything you read. If verification requires inspecting engine sources, read only, and keep the engine unchanged.

## Editing the engine after an explicit request

Treat `RobustToolbox/` as an escalation boundary.

Before editing engine code:

1. Confirm the behavior cannot be implemented in root content or an owning module.
2. Search for an existing public engine hook or content-side extension point.
3. Check whether a small content API addition can avoid an engine modification.
4. Explain why the engine change is required.

Good engine changes provide a generally reusable primitive that content cannot express. Bad engine changes move fork-specific gameplay into the engine, bypass existing APIs, or combine a small bug fix with unrelated engine refactoring.

Keep engine diffs narrow. Preserve upstream style and avoid changing engine internals merely because the content-side solution looks less elegant. When the change should be upstreamed, keep fork-specific references out of the implementation.

## Engine version and updates

The engine is a git submodule pinned to one commit. Reading the pin from the superproject is always allowed:

```powershell
git submodule status
```

Reading engine sources or running commands inside the submodule requires the explicit request described above.

The superproject pin and the tags present inside the submodule can disagree. Trust the recorded submodule commit, not a tag you remember.

Engine APIs change between releases: helper signatures, preferred overloads, ECS registration surfaces, container and interaction helpers, and serialization contracts all move. After an engine bump, re-verify every engine call you depend on instead of assuming your previous call still compiles. `RobustToolbox/RELEASE-NOTES.md` is the engine's own record of breaking changes.

Never run `git submodule update` to "fix" a mismatch, and never bump the engine pointer as part of an unrelated content change, even when it looks mechanical. A submodule pointer change requires its own explicit user request and is its own reviewed change.

Never treat the submodule working tree as owner-local. Every edit inside `RobustToolbox/` is an inherited upstream change and needs the current repository edit marker, an upstreamable justification, and a note about rebase cost.
