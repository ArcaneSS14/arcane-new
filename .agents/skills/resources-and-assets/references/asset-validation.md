
For every asset change verify:

- owning module and resource root;
- source URL or source repository revision;
- license and attribution;
- exact case-sensitive path;
- prototype and code references;
- RSI dimensions, states, and frame metadata;
- audio format and collection references;
- removal of replaced paths.

Use `RobustToolbox/Schemas/validate_rsis.py` for RSI work. Running this read-only validator is allowed by `.agents/rules/engine-boundaries.md`; modifying anything inside the engine is not. If the submodule is not populated, stop and report it instead of running `git submodule update` yourself.
