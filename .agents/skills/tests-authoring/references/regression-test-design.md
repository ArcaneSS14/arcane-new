
A regression test should contain the smallest setup that reproduces the old failure and an assertion that distinguishes fixed from broken behavior.

Distinguish a failure detecting the defect from setup, discovery or build failure. If an old revision cannot run the test, a compatible experiment restoring the faulty behavior can provide evidence, but report it as that experiment, not a run of the old revision. A compiler/analyzer test may legitimately check an expected diagnostic through its harness. Behavior-preserving refactors can correctly pass the same test before and after.

Good additional cases include:

- lower and upper boundaries;
- repeated call;
- rejected permission;
- entity deletion;
- cancellation;
- reconnect or late state;
- invalid prototype reference;
- stale client state;
- provider-specific database behavior.
