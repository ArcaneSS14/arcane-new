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
            var restartRate = defib.BpmZapHealFlatline;
            if (defib.AutoStabilisation && restartRate > 0f)
                restartRate = Math.Clamp(restartRate, heart.FibrillationCaps.X, heart.FibrillationCaps.Y);

            _heartRate.SetRate(uid, heart, restartRate, true);
            return;
        }

        _heartRate.UpdateRate(uid, heart, defib.BpmZapHeal, false, autoStabilisation: defib.AutoStabilisation);
    }
}
