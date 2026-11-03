
1. Identify every player-visible string introduced or changed.
2. Find the owner's locale subtree.
3. Add or update the source locale key.
4. Update Russian only when the user explicitly requests Russian localization. Otherwise leave existing `ru-RU` files untouched.
5. Pass required variables from code or prototypes. For changed arguments, attributes or references, trace caller names and value types through the resolved pattern and affected selector branches.
6. Confirm exact key and attribute usage. Parsing or `HasString` success alone does not prove resolution: the engine existence check may find the message without the requested attribute.
7. When resolution behavior changes, use an available targeted check with the normal resolver and relevant caller inputs; report what could not be exercised. A wording-only change needs the affected FTL and reference checks, not a full build or catalog-wide validation.

Do not silently invent uncertain translations. Mark the limitation in the handoff when a translation requires domain review.
