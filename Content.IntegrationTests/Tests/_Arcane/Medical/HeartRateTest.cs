#nullable enable
using Content.Arcane.Common.Medical.Heart;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Arcane.Shared.Medical.Heart;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Medical;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(HeartRateSystem))]
public sealed class HeartRateTest : GameTest
{
    private static readonly ProtoId<AlertPrototype> HeartStopAlert = "HeartStop";

    [SidedDependency(Side.Server)] private readonly HeartRateSystem _heart = null!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly SharedBuckleSystem _buckle = null!;
    [SidedDependency(Side.Server)] private readonly AlertsSystem _alerts = null!;
    [SidedDependency(Side.Server)] private readonly UserInterfaceSystem _ui = null!;
    [SidedDependency(Side.Server)] private readonly PowerCellSystem _powerCell = null!;
    [SidedDependency(Side.Server)] private readonly SharedBatterySystem _battery = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public void RateUpdatesRespectRestartCapsAndStabilisation()
    {
        var uid = SSpawn("OrganHumanHeart");
        var heart = SComp<HeartRateComponent>(uid);
        _heart.SetRate(uid, heart, 0f, false);
        _heart.UpdateRate(uid, heart, 5f, false);
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Stopped));

        _heart.UpdateRate(uid, heart, 5f, true);
        Assert.That(heart.CurrentRate, Is.EqualTo(5f));
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Fibrillating));

        _heart.UpdateRate(uid, heart, 100f, false, highCap: 80f);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Stable));

        _heart.SetRate(uid, heart, 250f, true);
        _heart.UpdateRate(uid, heart, 3f, false, highCap: 230f);
        Assert.That(heart.CurrentRate, Is.EqualTo(250f));

        _heart.SetRate(uid, heart, 30f, true);
        _heart.UpdateRate(uid, heart, -2f, false, lowCap: 55f);
        Assert.That(heart.CurrentRate, Is.EqualTo(30f));

        _heart.SetRate(uid, heart, 81f, true);
        _heart.UpdateRate(uid, heart, 5f, false, autoStabilisation: true);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));
        _heart.UpdateRate(uid, heart, 5f, false, autoStabilisation: true);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f), "Stabilising medicine must not oscillate around the resting rate.");
        _heart.UpdateRate(uid, heart, -5f, false, autoStabilisation: true);
        Assert.That(heart.CurrentRate, Is.EqualTo(75f), "A destabilising effect must retain its direction at the resting rate.");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CirculationAndStasisFollowThePatient()
    {
        var patient = SSpawn("MobHuman");
        var uid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(uid);

        var healthy = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref healthy);
        Assert.That(healthy.Found, Is.True);

        _heart.SetRate(uid, heart, 0f, false);
        var stopped = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref stopped);
        Assert.That(stopped.Found, Is.False);
        Assert.That(stopped.DoEffects, Is.True);

        var bed = SSpawn("StasisBed");
        Assert.That(_buckle.TryBuckle(patient, null, bed), Is.True);
        var stasis = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref stasis);
        Assert.That(stasis.DoEffects, Is.False);

        _body.RemoveOrgan(patient, uid);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(patient), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ExistingDefibrillatorsRestartTheNewHeartSimulation()
    {
        var patient = SSpawn("MobHuman");
        var uid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(uid);
        _heart.SetRate(uid, heart, 0f, false);

        var defib = SSpawn("DefibrillatorCompact");
        var ev = new TargetDefibrillatedEvent(patient, patient, (defib, SComp<DefibrillatorComponent>(defib)), []);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(210f));

        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(165f));

        _heart.SetRate(uid, heart, 90f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f), "An automatic shock must stop at the resting rate.");

        _heart.SetRate(uid, heart, 70f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ManualVoltageSettingsControlTheShock()
    {
        var patient = SSpawn("MobHuman");
        var heartUid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(heartUid);
        var defib = SSpawn("Defibrillator");
        var config = SComp<HeartDefibrillatorComponent>(defib);
        var zap = new TargetDefibrillatedEvent(patient, patient, (defib, SComp<DefibrillatorComponent>(defib)), []);

        var low = new DefibrillatorChargeSettingMessage(new() { Flips = [true, false, false, false] }) { Actor = patient };
        SEntMan.EventBus.RaiseLocalEvent(defib, low);
        Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var firstState), Is.True);
        Assert.That(firstState!.SettingsRevision, Is.EqualTo(1));
        SEntMan.EventBus.RaiseLocalEvent(patient, ref zap);
        Assert.That(heart.CurrentRate, Is.Zero, "One switch must slow the heart, without automatic stabilisation.");

        var restart = new DefibrillatorChargeSettingMessage(new() { Flips = [true, true, true, false] }) { Actor = patient };
        SEntMan.EventBus.RaiseLocalEvent(defib, restart);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref zap);
        Assert.That(heart.CurrentRate, Is.EqualTo(200f), "Three switches must restart a flatlined heart.");

        SEntMan.EventBus.RaiseLocalEvent(patient, ref zap);
        Assert.That(heart.CurrentRate, Is.EqualTo(350f), "A manual shock must retain its selected direction above the resting rate.");

        var malformed = new DefibrillatorChargeSettingMessage(new() { Flips = [] }) { Actor = patient };
        SEntMan.EventBus.RaiseLocalEvent(defib, malformed);
        Assert.That(config.BpmZapHeal, Is.EqualTo(150f), "Malformed switch arrays must not change the voltage.");
        Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var state), Is.True);
        Assert.That(state!.SettingsRevision, Is.EqualTo(3), "Rejected input must acknowledge the authoritative setting.");
        Assert.That(firstState.ChargeSetting.Flips, Is.EqualTo(new[] { true, false, false, false }),
            "Published UI states must not change when later settings are applied.");
    }

    [Test]
    public async Task MonitorTracksThePatientThroughHeartReplacement()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid defib = default;
        EntityUid heart = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            defib = SSpawnAtPosition("Defibrillator", TestMap.GridCoords);
            heart = _body.GetOrgan(patient, "Heart")!.Value;
            var scan = new AfterInteractEvent(patient, defib, patient, SComp<TransformComponent>(patient).Coordinates, true);
            SEntMan.EventBus.RaiseLocalEvent(defib, scan);
            Assert.That(scan.Handled, Is.False, "A defibrillator lying on the ground must not bypass the hands requirement.");

            _ui.OpenUi(defib, ManualDefibrillatorUiKey.Key, patient);
            scan = new AfterInteractEvent(patient, defib, patient, SComp<TransformComponent>(patient).Coordinates, true);
            SEntMan.EventBus.RaiseLocalEvent(defib, scan);
            Assert.That(scan.Handled, Is.True);
            Assert.That(SComp<ManualDefibrillatorComponent>(defib).DisplayedBpm, Is.EqualTo(80f));
            _body.RemoveOrgan(patient, heart);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            var monitor = SComp<ManualDefibrillatorComponent>(defib);
            Assert.That(monitor.DisplayedBpm, Is.Zero, "A removed heart must not keep the patient's monitor beating.");
            Assert.That(monitor.PulseState.Id, Is.EqualTo("Pulse0"));
            Assert.That(_body.InsertOrgan(patient, heart), Is.True);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            var monitor = SComp<ManualDefibrillatorComponent>(defib);
            Assert.That(monitor.DisplayedBpm, Is.EqualTo(MathF.Round(SComp<HeartRateComponent>(heart).CurrentRate)));
            _ui.CloseUi(defib, ManualDefibrillatorUiKey.Key, patient);
            Assert.That(monitor.MonitoredBody, Is.Null);
            Assert.That(monitor.DisplayedBpm, Is.Null);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DepletedManualDefibrillatorKeepsItsPowerSwitchInSync()
    {
        var user = SSpawn("MobHuman");
        var defib = SSpawn("Defibrillator");
        Assert.That(_powerCell.TryGetBatteryFromEntityOrSlot(defib, out var battery), Is.True);
        var cost = SComp<PowerCellDrawComponent>(defib).UseCharge;
        _battery.SetCharge(battery!.Value.AsNullable(), cost);

        var setting = new DefibrillatorChargeSetting { Power = true, Flips = [true, true, true, false] };
        var enable = new DefibrillatorChargeSettingMessage(setting) { Actor = user };
        SEntMan.EventBus.RaiseLocalEvent(defib, enable);
        var manual = SComp<ManualDefibrillatorComponent>(defib);
        var toggle = SComp<ItemToggleComponent>(defib);
        Assert.That(toggle.Activated, Is.True);
        var revision = manual.SettingsRevision;

        Assert.That(_powerCell.TryUseActivatableCharge(defib), Is.True);
        Assert.That(toggle.Activated, Is.False);
        Assert.That(manual.ChargeSetting.Power, Is.False);
        Assert.That(manual.SettingsRevision, Is.GreaterThan(revision));

        _battery.SetCharge(battery.Value.AsNullable(), cost * 2);
        _ui.OpenUi(defib, ManualDefibrillatorUiKey.Key, user);
        Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var state), Is.True);
        Assert.That(state!.ChargeSetting.Power, Is.False, "Charging must not leave a stale on-switch on a disabled device.");
        Assert.That(toggle.Activated, Is.False);

        SEntMan.EventBus.RaiseLocalEvent(defib, enable);
        Assert.That(toggle.Activated, Is.True, "The charged device must be usable after switching it back on.");
        var noVoltage = new DefibrillatorChargeSettingMessage(new() { Power = true }) { Actor = user };
        SEntMan.EventBus.RaiseLocalEvent(defib, noVoltage);
        Assert.That(toggle.Activated, Is.False);
        Assert.That(manual.ChargeSetting.Power, Is.True, "Turning off the last voltage switch must preserve the selected power-switch position.");
        _ui.CloseUi(defib, ManualDefibrillatorUiKey.Key, user);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void HeartTransplantsRefreshAlerts()
    {
        var patient = SSpawn("MobHuman");
        var uid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(uid);
        Assert.That(_body.RemoveOrgan(patient, uid), Is.True);
        Assert.That(_alerts.IsShowingAlert(patient, HeartStopAlert), Is.True);

        Assert.That(_body.InsertOrgan(patient, uid), Is.True);
        Assert.That(_alerts.IsShowingAlert(patient, HeartStopAlert), Is.False);

        Assert.That(_body.RemoveOrgan(patient, uid), Is.True);
        _heart.SetRate(uid, heart, 0f, false);
        Assert.That(_body.InsertOrgan(patient, uid), Is.True);
        Assert.That(_alerts.IsShowingAlert(patient, HeartStopAlert), Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    [RunOnSide(Side.Server)]
    public void BrainReinsertionKeepsTheAppropriateMissingHeartBehavior(bool hasCirculation)
    {
        var patient = SSpawn("MobHuman");
        if (!hasCirculation)
            SEntMan.RemoveComponent<BloodstreamComponent>(patient);

        var heart = _body.GetOrgan(patient, "Heart")!.Value;
        var brain = _body.GetOrgan(patient, "Brain")!.Value;
        Assert.That(_body.RemoveOrgan(patient, heart), Is.True);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(patient), Is.EqualTo(!hasCirculation));

        Assert.That(_body.RemoveOrgan(patient, brain), Is.True);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(patient), Is.True);
        Assert.That(_body.InsertOrgan(patient, brain), Is.True);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(patient), Is.EqualTo(!hasCirculation));
        Assert.That(_body.InsertOrgan(patient, heart), Is.True);
    }

    [Test]
    public async Task FullBloodVolumeDoesNotCureBradycardia()
    {
        var hearts = new List<(EntityUid Uid, float UpperBound)>();
        await Server.WaitPost(() =>
        {
            foreach (var (rate, upperBound) in new[] { (30f, 40f), (70f, 80f) })
            {
                var patient = SSpawn("MobHuman");
                var heartUid = _body.GetOrgan(patient, "Heart")!.Value;
                _heart.SetRate(heartUid, SComp<HeartRateComponent>(heartUid), rate, true);
                hearts.Add((heartUid, upperBound));
            }
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            foreach (var (uid, upperBound) in hearts)
            {
                Assert.That(SComp<HeartRateComponent>(uid).CurrentRate, Is.GreaterThan(0f).And.LessThan(upperBound),
                    "A full bloodstream must not instantly restore a low pulse to its resting rate.");
            }
        });
    }

    [Test]
    public async Task NaturalStabilisationLeavesStableBoundsAndStopsAtNormal()
    {
        var hearts = new Dictionary<float, EntityUid>();
        await Server.WaitPost(() =>
        {
            foreach (var offset in new[] { -40f, 130f, 0.05f })
            {
                var patient = SSpawn("MobHuman");
                var uid = _body.GetOrgan(patient, "Heart")!.Value;
                var heart = SComp<HeartRateComponent>(uid);
                _heart.SetRate(uid, heart, heart.NormalRate + offset, true);
                hearts.Add(offset, uid);
            }
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            foreach (var (offset, uid) in hearts)
            {
                var heart = SComp<HeartRateComponent>(uid);
                Assert.That(MathF.Abs(heart.CurrentRate - heart.NormalRate), Is.LessThan(MathF.Abs(offset)));
            }
            var nearNormal = SComp<HeartRateComponent>(hearts[0.05f]);
            Assert.That(nearNormal.CurrentRate, Is.EqualTo(nearNormal.NormalRate));
        });
        await RunSeconds(2.1f);
        await Server.WaitPost(() =>
        {
            var nearNormal = SComp<HeartRateComponent>(hearts[0.05f]);
            Assert.That(nearNormal.CurrentRate, Is.EqualTo(nearNormal.NormalRate));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AutomaticRestartRespectsTheImplantedHeart()
    {
        var patient = SSpawn("MobHuman");
        var uid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(uid);
        var defib = SSpawn("DefibrillatorCompact");
        _heart.SetRate(uid, heart, 0f, false);
        var zap = new TargetDefibrillatedEvent(patient, patient, (defib, SComp<DefibrillatorComponent>(defib)), []);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref zap);
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Stable));
        Assert.That(heart.CurrentRate, Is.GreaterThan(0f));

        var donor = SSpawn("OrganMothHeart");
        Assert.That(_body.RemoveOrgan(patient, uid), Is.True);
        Assert.That(_body.InsertOrgan(patient, donor), Is.True);
        var transplanted = SComp<HeartRateComponent>(donor);
        _heart.SetRate(donor, transplanted, 0f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref zap);
        Assert.That(_heart.GetState(transplanted), Is.EqualTo(HeartState.Stable));
        Assert.That(transplanted.CurrentRate, Is.EqualTo(transplanted.FibrillationCaps.Y));
    }

    [Test]
    public async Task ShockReplacesThePreviousPatientEvenWithoutAHeart()
    {
        await Pair.CreateTestMap();
        EntityUid defib = default;
        EntityUid second = default;
        await Server.WaitPost(() =>
        {
            var first = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            second = SSpawnAtPosition("MobHuman", TestMap.GridCoords);
            var heart = _body.GetOrgan(second, "Heart")!.Value;
            Assert.That(_body.RemoveOrgan(second, heart), Is.True);
            defib = SSpawnAtPosition("Defibrillator", TestMap.GridCoords);
            _ui.OpenUi(defib, ManualDefibrillatorUiKey.Key, first);
            var scan = new AfterInteractEvent(first, defib, first, SComp<TransformComponent>(first).Coordinates, true);
            SEntMan.EventBus.RaiseLocalEvent(defib, scan);
            Assert.That(SComp<ManualDefibrillatorComponent>(defib).DisplayedBpm, Is.EqualTo(80f));

            var zap = new TargetDefibrillatedEvent(first, second, (defib, SComp<DefibrillatorComponent>(defib)), []);
            SEntMan.EventBus.RaiseLocalEvent(second, ref zap);
            var monitor = SComp<ManualDefibrillatorComponent>(defib);
            Assert.That(monitor.MonitoredBody, Is.EqualTo(second));
            Assert.That(monitor.DisplayedBpm, Is.Zero);
            Assert.That(monitor.PulseState.Id, Is.EqualTo("Pulse3000"));
        });

        await RunSeconds(2.1f);
        await Server.WaitPost(() =>
        {
            var monitor = SComp<ManualDefibrillatorComponent>(defib);
            Assert.That(monitor.MonitoredBody, Is.EqualTo(second));
            Assert.That(monitor.DisplayedBpm, Is.Zero);
            Assert.That(monitor.PulseState.Id, Is.EqualTo("Pulse0"));
        });
    }


}
