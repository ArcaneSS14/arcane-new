using Content.Arcane.Common.AlmanacBlade;
using Content.Shared.EntityEffects;

namespace Content.Arcane.Shared.AlmanacBlade;

public sealed partial class RecordAlmanacFlip : EntityEffectBase<RecordAlmanacFlip>;

public sealed partial class RecordAlmanacFlipEffectSystem : EntityEffectSystem<MetaDataComponent, RecordAlmanacFlip>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RecordAlmanacFlip> args)
    {
        if (_net.IsClient)
            return;

        var ev = new AlmanacFlipPerformedEvent();
        RaiseLocalEvent(ref ev);
    }
}
