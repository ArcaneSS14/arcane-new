using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Body;
using Content.Shared.Medical;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class HeartDefibrillatorSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private BodySystem _body = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnDefibrillated);
    }

    private void OnDefibrillated(Entity<BodyComponent> ent, ref TargetDefibrillatedEvent args)
    {
        if (!TryComp<HeartDefibrillatorComponent>(args.Defibrillator, out var defib)
            || _body.GetOrgan(ent, Heart) is not { } uid
            || !TryComp<HeartRateComponent>(uid, out var heart))
            return;

        if (_heartRate.GetState(heart) == HeartState.Stopped)
        {
            _heartRate.SetRate(uid, heart, defib.BpmZapHealFlatline, true);
            return;
        }

        var sign = heart.CurrentRate > heart.NormalRate && defib.AutoStabilisation ? -1 : 1;
        _heartRate.UpdateRate(uid, heart, sign * defib.BpmZapHeal, false);
    }
}
