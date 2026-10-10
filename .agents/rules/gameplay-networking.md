
Use `.agents/rules/interactions-and-authority.md` for authorization and authoritative execution. This rule covers how that execution crosses the network.

Represent requests as intent, not client-claimed results, and send only the data needed to express that intent. Verify whether the current API requires `NetEntity` rather than raw `EntityUid` in network payloads. Do not replicate hidden state for UI convenience; define a limited state object or explicit response.

After authoritative mutation, dirty replicated state and let the owning client path refresh presentation. Shared contracts may support prediction; predicted effects must be deterministic and safe to reconcile, and must not commit privileged outcomes or irreversible side effects.
