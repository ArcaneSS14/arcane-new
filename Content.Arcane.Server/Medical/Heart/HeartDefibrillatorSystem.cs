using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Arcane.Server.Medical.Defibrillator;
using Content.Shared.Body;
using Content.Shared.Medical;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class HeartDefibrillatorSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private ManualDefibrillatorMonitorSystem _monitor = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnDefibrillated);
    }

    private void OnDefibrillated(Entity<BodyComponent> ent, ref TargetDefibrillatedEvent args)
    {
        if (!TryComp<HeartDefibrillatorComponent>(args.Defibrillator, out var defib))
            return;

        var bpm = 0f;
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

            bpm = heart.CurrentRate;
        }

        if (TryComp<ManualDefibrillatorComponent>(args.Defibrillator, out var manual))
            _monitor.ShowShock((args.Defibrillator, manual), ent.Owner, bpm);
    }
}
