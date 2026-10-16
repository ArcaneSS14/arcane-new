using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Arcane.Server.Medical.Defibrillator;
using Content.Shared.Body;
using Content.Shared.Medical;

namespace Content.Arcane.Server.Medical.Heart;

public sealed partial class HeartDefibrillatorSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ManualDefibrillatorSystem _manualDefibrillator = default!;

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

        if (TryComp<ManualDefibrillatorComponent>(args.Defibrillator, out var manual))
        {
            manual.PulseState = "Pulse3000";
            _manualDefibrillator.UpdateUi((args.Defibrillator, manual));
        }

        if (_heartRate.GetState(heart) == HeartState.Stopped)
        {
            _heartRate.SetRate(uid, heart, defib.BpmZapHealFlatline, true);
            return;
        }

        _heartRate.UpdateRate(uid, heart, defib.BpmZapHeal, false, autoStabilisation: defib.AutoStabilisation);
    }
}
