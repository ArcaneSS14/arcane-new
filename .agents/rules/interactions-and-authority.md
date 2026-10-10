
Every player-triggered operation needs an explicit authority path. Client checks provide immediate feedback, not security. Shared behavior may run predictively when its effects are deterministic and safe to reconcile; prediction must not commit protected outcomes or irreversible side effects.

For each client-originated intent, trace the concrete dispatch path and verify which checks the framework guarantees. The server path must validate remaining gameplay authorization, ownership, submitted values, and current-state requirements before authoritative mutation. UI messages, actions, verbs, interactions, and network events all re-enter server validation.

Prefer `On... -> Try... -> Can... -> Execute...`. `Can...` must not mutate. `Try...` must not assume the caller already validated. Mutate once in the authoritative path, dirty replicated state, and refresh affected UI through its normal state path.

When an action is interruptible, model cancellation and completion explicitly. Do not charge resources, award outcomes, or persist state before the authoritative completion point unless rollback is defined.
