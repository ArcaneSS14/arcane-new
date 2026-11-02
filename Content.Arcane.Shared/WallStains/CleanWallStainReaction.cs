using Content.Arcane.Shared.WallStains.Components;
using Content.Shared.EntityEffects;

namespace Content.Arcane.Shared.WallStains;

public sealed partial class CleanWallStainReaction : EntityEffect
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user,
        bool predicted = true)
    {
        if (!IoCManager.Resolve<IEntityManager>().HasComponent<StainedWallComponent>(target))
            return;

        IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(target, new CleanWallStainsEvent(true));
    }
}
