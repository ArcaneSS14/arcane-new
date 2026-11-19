using Content.Shared.EntityEffects;
using Content.Shared.Stacks;

namespace Content.Arcane.Shared.Xenobiology;

public sealed partial class SpawnExtractStack : EntityEffectBase<SpawnExtractStack>
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField(required: true)]
    public int Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager systems)
    {
        var stack = prototype.Index(Stack);
        return Loc.GetString("entity-effect-guidebook-spawn-entity",
            ("chance", Probability), ("entname", prototype.Index(stack.Spawn).Name), ("amount", Amount));
    }
}
