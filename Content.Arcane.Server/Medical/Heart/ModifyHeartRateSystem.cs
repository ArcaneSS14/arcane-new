using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class ModifyHeartRateSystem : EntityEffectSystem<BodyComponent, ModifyHeartRate>
{
    [Dependency] private HeartRateSystem _heartRate = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<ModifyHeartRate> args)
    {
        if (_heartRate.GetHeart(ent) is not { Comp: { } heart } heartEntity)
            return;

        _heartRate.UpdateRate(heartEntity.Owner,
            heart,
            args.Scale * args.Effect.Amount,
            args.Effect.HeartRestart,
            args.Effect.LowerCap,
            args.Effect.HigherCap,
            args.Effect.AutoStabilisation);
    }
}
