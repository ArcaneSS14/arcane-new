
Trace from prototype type to component data fields and from every resource reference to an existing path.

Review:

- duplicate IDs;
- incorrect parent chains;
- abstract versus spawnable intent;
- component replacement versus inheritance;
- invalid enum values;
- stale source-fork fields;
- missing FTL;
- missing RSI state or sound file;
- module resource ownership.

Use a targeted validator for the changed prototype and directly referenced resources when one exists. The general YAML linter is a broad repository check; run it only when requested or when the task specifically requires that workflow, following root `AGENTS.md` and `.agents/rules/verification.md`.
