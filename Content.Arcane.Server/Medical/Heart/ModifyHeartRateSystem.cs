using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class ModifyHeartRateSystem : EntityEffectSystem<BodyComponent, ModifyHeartRate>
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<ModifyHeartRate> args)
    {
        if (_body.GetOrgan(ent, HeartCategory) is not { } heartUid
            || !TryComp<HeartRateComponent>(heartUid, out var heart))
            return;

        _heartRate.UpdateRate(heartUid,
            heart,
            args.Scale * args.Effect.Amount,
            args.Effect.HeartRestart,
            args.Effect.LowerCap,
            args.Effect.HigherCap,
            args.Effect.AutoStabilisation);
    }
}
