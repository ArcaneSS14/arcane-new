using Content.Arcane.Shared.Xenobiology;
using Content.Server.Stack;
using Content.Shared.EntityEffects;

namespace Content.Arcane.Server.Xenobiology;

public sealed partial class SpawnExtractStackSystem : EntityEffectSystem<TransformComponent, SpawnExtractStack>
{
    [Dependency] private StackSystem _stack = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnExtractStack> args)
    {
        if (args.Effect.Amount > 0)
            _stack.SpawnMultipleNextToOrDrop(args.Effect.Stack, args.Effect.Amount, entity);
    }
}
