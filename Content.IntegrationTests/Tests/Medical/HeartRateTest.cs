// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Arcane.Shared.Medical.Heart;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical;
using Content.Shared.Interaction;
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
        Assert.That(SComp<BloodstreamComponent>(SSpawn("MobDiona")).RequiresHeart, Is.False);
        Assert.That(SComp<BloodstreamComponent>(SSpawn("MobSlimePerson")).RequiresHeart, Is.False);

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
            Assert.That(monitor.DisplayedBpm, Is.EqualTo(SComp<HeartRateComponent>(heart).CurrentRate));
            _ui.CloseUi(defib, ManualDefibrillatorUiKey.Key, patient);
            Assert.That(monitor.MonitoredBody, Is.Null);
            Assert.That(monitor.DisplayedBpm, Is.Null);
        });
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

    [TestCase("MobSlimeXenobioBaby")]
    [TestCase("MobBlobPod")]
    [RunOnSide(Side.Server)]
    public void ExistingAnatomiesHaveWorkingCirculation(string prototype)
    {
        var patient = SSpawn(prototype);
        var blood = SComp<BloodstreamComponent>(patient);
        if (!blood.RequiresHeart)
            return;

        var circulation = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref circulation);
        Assert.That(circulation.Found || !circulation.DoEffects, Is.True,
            "Existing mobs must not start taking missing-heart damage on spawn.");
    }
}
// Arcane-End
