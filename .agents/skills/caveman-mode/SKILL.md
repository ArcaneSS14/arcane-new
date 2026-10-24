---
name: caveman-mode
description: Apply an explicit, current-turn-only concise response mode without changing task rules, verification, or technical payload.
---

## Когда применять

Use only when the user explicitly requests `$caveman`, “caveman mode”, a brief/minimal answer, fewer tokens, or an unambiguous equivalent. It changes response style for the current turn only; it never persists into later turns.

## До изменения

Identify whether the response includes a security warning, destructive or irreversible action, clarification question, ordered procedure, commit/PR/issue text, documentation, or another persistent artifact. Those surfaces need normal complete prose.

## Правила и границы

- Lead with the result, then only the necessary reason and next action. Omit greetings, filler, empty recap, and decorative tables.
- Preserve the user's language. Do not shorten code blocks, commands, paths, API names, numbers, errors, citations, negations, safety constraints, acceptance criteria, or verification results.
- Use complete ordinary sentences whenever compression could create ambiguity.
- Arcane system, developer, `AGENTS.md`, rule, skill, and user instructions outrank this output mode. The mode never authorizes skipping commentary, evidence, verification, warnings, or required delivery details.
- Stop compacting for security, irreversible work, clarification, complex ordering, documentation, commits, PR descriptions, issues, and all other persistent artifacts. State the necessary detail in full.

### Minimal examples

Correct: “`git diff --check` passed; no files outside scope changed.” Prohibited: omit a failed check or a destructive-action warning to save words. Stop: use normal prose when a concise answer could obscure risk or sequence.

## Выбор подхода

Use one short result paragraph for a simple completed request. Use a compact list only when it preserves several independent facts. Return to normal response style on the next turn unless the user explicitly requests this mode again.

## Антипаттерны

- Do not imitate a caveman persona, break grammar, invent abbreviations, or add branding.
- Do not persist mode state, install/run upstream tooling, or route a task to an external service.
- Do not replace domain skills, rewrite payload, or claim token savings without local measurement.

## Проверка

Before sending, verify that every negation, number, command, path, error, safety condition, and requested result remains exact. Confirm the response still contains required status updates and verification evidence.

## References

- [Vendored Caveman skill](../../vendor/caveman/skills/caveman/SKILL.md): Read this only when comparing the adapter with the pinned upstream wording or refreshing the vendor snapshot.
