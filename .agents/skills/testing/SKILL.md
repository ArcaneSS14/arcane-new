---
name: testing
description: Select the existing owner test project and run checks matching the real failure mode.
---

# Select and run sufficient checks

Match the changed contract and failure mode to its existing owner under [verification](../../rules/verification.md). Content.Tests and Content.IntegrationTests are real root owners; use the actual project graph rather than inventing module test projects or new fixtures/CI steps.

Use the [testing matrix](references/testing-matrix.md) for an initial choice. Test through accessible production APIs/events. Follow the affected workflow's exact configuration and artifact when reproducing CI; gates may target a DLL or a csproj. Confirm artifact freshness before --no-build and inspect selected/executed counts. Integration runs use DOTNET_gcServer=1.

Start with the smallest sufficient check; broaden for a concrete uncovered risk or required gate while respecting the current task's local/remote restrictions. Do not restore dependencies or run full root/module suites by default. A parse-only check cannot establish network or gameplay behavior; static inspection does not establish a visual result.

Record exact commands, relevant results/failures and omissions. Use [tests-authoring](../tests-authoring/SKILL.md) when a new or changed test is actually needed.
