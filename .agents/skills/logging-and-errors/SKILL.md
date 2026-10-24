---
name: logging-and-errors
description: Add useful diagnostics, choose log levels, preserve context, and handle player-facing failures safely.
---

# Operator diagnostics and player feedback

Choose levels by meaning: debug/verbose for development detail, information for operational transitions, warning for unexpected recoverable state, error for failed operations needing investigation. Expected rejection is not automatically an error.

Include searchable subsystem/operation, entity/prototype identity and useful non-sensitive parameters. [Security](../security-and-validation/SKILL.md) owns secret/private-data limits. Technical diagnostics may need IDs; player messages should use safe localized feedback through [forms](../forms-and-input-validation/SKILL.md).

Catch only to recover, add context, translate a result or clean up; preserve stack information on rethrow and do not swallow the failure. Bound or aggregate repeated diagnostics in hot loops or abusive request paths.

Check the changed log/exception branch and data exposure under [verification](../../rules/verification.md). Changing one level does not require every runtime success/rejection path to be exercised.
