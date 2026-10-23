using Content.Arcane.Common.Medical.Heart;
using Content.Arcane.Shared.Medical.Heart;

namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Lets active compressions support an installed heart without supplying missing blood.
/// </summary>
public sealed partial class CPRCirculationSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heart = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CPRPerfusionComponent, FindWorkingHeartEvent>(OnFindHeart);
    }

    private void OnFindHeart(Entity<CPRPerfusionComponent> ent, ref FindWorkingHeartEvent args)
    {
        args.Found |= _heart.GetHeart(ent) != null;
    }
}
