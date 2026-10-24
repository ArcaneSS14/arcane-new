---
name: randomness-and-determinism
description: Use authoritative, deterministic, weighted, and testable randomness without breaking prediction or reproducibility.
---

Randomness is part of authority and simulation behavior, not a convenience call.

## Choose the owner

Protected outcomes such as rewards, loot, combat results, economy values, role assignment, or hidden selections are chosen on the server. Client randomness is limited to presentation that has no gameplay effect.

Shared predicted logic must not use an unsynchronized random source. Either derive the result deterministically from shared inputs or let authoritative state correct presentation without duplicating side effects.

## Data-driven selection

Use existing random services, weighted collections, datasets, and prototype-driven tables. Validate empty collections, zero or negative weights, duplicate entries, and missing prototype references.

## Reproducibility

For tests and debugging, control the random source or inject deterministic inputs. Do not depend on global random state or test ordering.

## Common failures

- client selects a protected result;
- prediction and server choose different values;
- repeated retries bias a result;
- empty collection crashes;
- a weight change silently alters balance;
- tests are flaky because randomness is uncontrolled.

## Verification

Select only cases exercising the changed contract under [verification](../../rules/verification.md); the following list is conditional, not a required full suite.

Test empty, single-entry, weighted boundary, repeated invocation, rejected requests, deterministic test seeds, and authoritative reconciliation.

For a changed choice establish who owns the outcome, who may know its candidates, whether retry/replay can reroll it, and whether the chosen result persists or replicates. Cosmetic randomness may be client-local; protected targets/rewards/roles may not.
