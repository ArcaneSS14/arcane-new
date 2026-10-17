#nullable enable
using Content.Arcane.Common.Medical.Heart;
using System.Linq;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Arcane.Shared.Medical.Heart;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Medical;

[TestOf(typeof(HeartRateSystem))]
public sealed class HeartRateTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Epinephrine = "Epinephrine";
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
    public void StoppedHeartRequiresExplicitRestart()
    {
        var uid = SSpawn("OrganHumanHeart");
        var heart = SComp<HeartRateComponent>(uid);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));

        _heart.SetRate(uid, heart, 0f, false);
        _heart.UpdateRate(uid, heart, 5f, false);
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Stopped));

        _heart.UpdateRate(uid, heart, 5f, true);
        Assert.That(heart.CurrentRate, Is.EqualTo(5f));
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Fibrillating));

        _heart.UpdateRate(uid, heart, 100f, false, highCap: 80f);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));
        Assert.That(_heart.GetState(heart), Is.EqualTo(HeartState.Stable));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CirculationAndStasisFollowThePatient()
    {
        var patient = SSpawn("MobHuman");
        var uid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(uid);

        var healthy = new FindWorkingHeartEvent();
        Assert.That(healthy.DoEffects, Is.False, "The common hook must be neutral without the heart simulation.");
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
    public void SourceConfigurationIsMergedIntoExistingPrototypes()
    {
        Assert.That(SComp<HeartRateComponent>(SSpawn("OrganMothHeart")).NormalRate, Is.EqualTo(65f));
        Assert.That(SComp<HeartRateComponent>(SSpawn("OrganHeartSandevistan")).CriticalRate, Is.EqualTo(400f));
        Assert.That(SComp<HeartCirculationComponent>(SSpawn("MobDiona")).RequiresHeart, Is.False);
        Assert.That(SComp<HeartCirculationComponent>(SSpawn("MobSlimePerson")).RequiresHeart, Is.False);

        var epinephrine = SProtoMan.Index(Epinephrine);
        var effects = epinephrine.Metabolisms!.Metabolisms["Bloodstream"].Effects;
        Assert.That(effects.OfType<ModifyHeartRate>().Single().HeartRestart, Is.True);
        Assert.That(effects.Length, Is.GreaterThan(1), "The partial must preserve the existing reagent effects.");
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
    public void RateCapsLimitEffectsWithoutReversingThem()
    {
        var uid = SSpawn("OrganHumanHeart");
        var heart = SComp<HeartRateComponent>(uid);
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

    [TestCase(30f, 40f)]
    [TestCase(70f, 80f)]
    public async Task FullBloodVolumeDoesNotCureBradycardia(float rate, float upperBound)
    {
        EntityUid heartUid = default;
        await Server.WaitPost(() =>
        {
            var patient = SSpawn("MobHuman");
            heartUid = _body.GetOrgan(patient, "Heart")!.Value;
            _heart.SetRate(heartUid, SComp<HeartRateComponent>(heartUid), rate, true);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            var heart = SComp<HeartRateComponent>(heartUid);
            Assert.That(heart.CurrentRate, Is.GreaterThan(0f).And.LessThan(upperBound),
                "A full bloodstream must not instantly restore a low pulse to its resting rate.");
        });
    }

    [TestCase("MobSlimeXenobioBaby")]
    [TestCase("MobBlobPod")]
    [RunOnSide(Side.Server)]
    public void ExistingAnatomiesHaveWorkingCirculation(string prototype)
    {
        var patient = SSpawn(prototype);
        if (!_heart.RequiresHeart(patient))
            return;

        var circulation = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref circulation);
        Assert.That(circulation.Found || !circulation.DoEffects, Is.True,
            "Existing mobs must not start taking missing-heart damage on spawn.");
    }

    [TestCase(40f)]
    [TestCase(210f)]
    [TestCase(80.05f)]
    public async Task NaturalStabilisationLeavesStableBoundsAndStopsAtNormal(float initialRate)
    {
        EntityUid uid = default;
        await Server.WaitPost(() =>
        {
            var patient = SSpawn("MobHuman");
            uid = _body.GetOrgan(patient, "Heart")!.Value;
            _heart.SetRate(uid, SComp<HeartRateComponent>(uid), initialRate, true);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            var rate = SComp<HeartRateComponent>(uid).CurrentRate;
            Assert.That(MathF.Abs(rate - 80f), Is.LessThan(MathF.Abs(initialRate - 80f)));
            _heart.SetRate(uid, SComp<HeartRateComponent>(uid), 80.05f, true);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() => Assert.That(SComp<HeartRateComponent>(uid).CurrentRate, Is.EqualTo(80f)));
        await RunSeconds(2.1f);
        await Server.WaitPost(() => Assert.That(SComp<HeartRateComponent>(uid).CurrentRate, Is.EqualTo(80f)));
    }

    [TestCase("MobMoth")]
    [TestCase("MobDwarf")]
    [TestCase("MobVox")]
    [TestCase("MobDecapoid")]
    [RunOnSide(Side.Server)]
    public void AutomaticRestartRespectsTheImplantedHeart(string prototype)
    {
        var patient = SSpawn(prototype);
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

    [Test]
    public async Task MonitorOnlyPublishesChangesToVisiblePulseAndBpm()
    {
        await Pair.CreateTestMap();
        EntityUid defib = default;
        EntityUid heartUid = default;
        DefibrillatorBuiState initialState = null!;
        await Server.WaitPost(() =>
        {
            var patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            heartUid = _body.GetOrgan(patient, "Heart")!.Value;
            var heart = SComp<HeartRateComponent>(heartUid);
            heart.RateUpdateModifier = 0f;
            _heart.SetRate(heartUid, heart, 80.1f, true);
            defib = SSpawnAtPosition("Defibrillator", TestMap.GridCoords);
            _ui.OpenUi(defib, ManualDefibrillatorUiKey.Key, patient);
            var scan = new AfterInteractEvent(patient, defib, patient, SComp<TransformComponent>(patient).Coordinates, true);
            SEntMan.EventBus.RaiseLocalEvent(defib, scan);
            Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var state), Is.True);
            initialState = state!;
            Assert.That(initialState.Bpm, Is.EqualTo(80f));
            _heart.SetRate(heartUid, heart, 80.2f, true);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var state), Is.True);
            Assert.That(state, Is.SameAs(initialState), "A change hidden by rounding must not publish a new UI state.");
            Assert.That(SComp<HeartRateComponent>(heartUid).CurrentRate, Is.EqualTo(80.2f));
            _heart.SetRate(heartUid, SComp<HeartRateComponent>(heartUid), 80f, true);
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() =>
        {
            Assert.That(_ui.TryGetUiState<DefibrillatorBuiState>(defib, ManualDefibrillatorUiKey.Key, out var state), Is.True);
            Assert.That(state, Is.Not.SameAs(initialState), "An animation change must publish even when rounded BPM is unchanged.");
            Assert.That(state!.PulseState.Id, Is.EqualTo("Pulse80"));
            _heart.SetRate(heartUid, SComp<HeartRateComponent>(heartUid), 310f, true);
            SComp<HeartRateComponent>(heartUid).CriticalStopChance = 0f;
        });

        await RunSeconds(1.1f);
        await Server.WaitPost(() => Assert.That(SComp<ManualDefibrillatorComponent>(defib).PulseState.Id, Is.EqualTo("Pulse320")));
    }
}
