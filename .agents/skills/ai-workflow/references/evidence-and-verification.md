
Choose evidence for the claim rather than applying a fixed ranking:

- Use current declarations, callers, configuration, and the reviewed revision for API, access, ownership, and effective configuration claims.
- Use a relevant executed test or reproducible runtime observation for the behavior it actually covers. A passing test or green CI does not refute a source-proved defect on an uncovered path; a source read does not prove rendered output or live behavior.
- Use version-specific official documentation for the relevant tool/runtime contract, and history or source PR discussion for intent and provenance. Confirm applicability to the selected revision.
- Nearby code suggests a pattern; memory or an assumption suggests a question. Verify either before treating it as a current fact.

State what was observed, what is inferred, and what remains unverified. Re-check paths and APIs after branch updates. For a failed command, preserve the exact failure and avoid claiming downstream checks were performed.
