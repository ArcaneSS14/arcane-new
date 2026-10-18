using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Body;
using Content.Shared.Medical;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class HeartDefibrillatorSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heartRate = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnDefibrillated);
    }

    private void OnDefibrillated(Entity<BodyComponent> ent, ref TargetDefibrillatedEvent args)
    {
        if (!TryComp<HeartDefibrillatorComponent>(args.Defibrillator, out var defib))
            return;

        if (_heartRate.GetHeart(ent) is { Comp: { } heart } heartEntity)
        {
            if (_heartRate.GetState(heart) == HeartState.Stopped)
            {
                var restartRate = defib.BpmZapHealFlatline;
                if (defib.AutoStabilisation && restartRate > 0f)
                    restartRate = Math.Clamp(restartRate, heart.FibrillationCaps.X, heart.FibrillationCaps.Y);

                _heartRate.SetRate(heartEntity.Owner, heart, restartRate, true);
            }
            else
                _heartRate.UpdateRate(heartEntity.Owner, heart, defib.BpmZapHeal, false, autoStabilisation: defib.AutoStabilisation);
        }
    }
}
