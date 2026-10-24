
For the changed asset type select relevant checks:

- owning module and resource root;
- source URL or source repository revision;
- license and attribution;
- exact case-sensitive path;
- prototype and code references;
- RSI dimensions, states, and frame metadata;
- audio format and collection references;
- removal of replaced paths.

For RSI work inspect dimensions, directions/states/frame metadata and use the current `RobustToolbox/Schemas/validate_rsis.py` arguments. Audio work instead checks format/collections/playback contract; maps use their serializer/loader. Running this read-only validator is allowed by `.agents/rules/engine-boundaries.md`; modifying anything inside the engine is not. If the submodule is not populated, stop and report it instead of running `git submodule update` yourself.
