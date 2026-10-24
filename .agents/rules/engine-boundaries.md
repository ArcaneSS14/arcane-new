# Engine boundaries

RobustToolbox/ is a pinned upstream submodule. Addressed read-only source inspection to verify a declaration, API or render contract is allowed. Read the pin from the superproject with git submodule status; use the recorded commit rather than a remembered tag.

Without a current explicit engine request, do not:

- edit, create, remove, move or rename files inside RobustToolbox/;
- run commands with their working directory inside the submodule, including git -C RobustToolbox;
- update, stage or move the gitlink, or run git submodule update;
- bypass content access through private engine implementation, reflection or copied internals.

CI's read-only validators against content are an exception for execution from the superproject, e.g. python3 RobustToolbox/Schemas/validate_rsis.py Resources/. This permits the validator run, never modifying it or other submodule commands.

When access blocks content work, verify the declaration, modifier and caller/owner assemblies. Use an existing legitimate content-side extension; if none exists, report the smallest reusable hook needed and wait for the user. Convenience is not authorization to change the engine.

An explicit request covers its named engine change, not unrelated later work. Before that change, check content alternatives and explain the boundary. Keep the engine diff narrow, upstreamable where appropriate, and separate from fork gameplay. A gitlink update needs its own explicit scope.

After an engine bump, recheck the calls and generated contracts actually used by the task; breaking changes are recorded in RobustToolbox/RELEASE-NOTES.md. Do not update the submodule to repair an unexplained mismatch.
