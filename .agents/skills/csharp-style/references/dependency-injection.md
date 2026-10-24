# Dependency injection shape

Match the current owning file and actual declaration, not a frequency table or a guessed manager name. A common system field is:

```csharp
[Dependency] private IGameTiming _timing = default!;
```

Existing abstract bases may deliberately expose protected dependencies, e.g. protected IGameTiming Timing in Content.Trauma.Shared/Heretic/Systems/SharedHereticCombatMarkSystem.cs. This is not a universal private-only or readonly rule. Do not normalize another file's established visibility/naming while making an unrelated change.

Use the repository's supported injected/system dependency mechanism rather than repeated IoCManager.Resolve inside methods. Verify interface, namespace, assembly and client/server/shared variant before calling it; an injected name is not proof of an API.

Common decision categories are timing, random, entity/prototype/configuration services and owning transform/appearance/audio/UI/popups/containers systems. Find the existing owner near the affected code instead of choosing one by historical declaration counts. Follow [ECS conventions](../../../rules/ecs-writing-conventions.md) and [API/access](../../../rules/coding-and-api-design.md).
