#nullable enable
using Content.Arcane.Common.Medical.Heart;
using System.Linq;
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
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(HeartRateSystem))]
public sealed class HeartRateTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Epinephrine = "Epinephrine";
    private static readonly ProtoId<AlertPrototype> HeartStopAlert = "HeartStop";

    [SidedDependency(Side.Server)] private readonly HeartRateSystem _heart = null!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly SharedBuckleSystem _buckle = null!;
    [SidedDependency(Side.Server)] private readonly AlertsSystem _alerts = null!;

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

        var defib = SSpawn("Defibrillator");
        var ev = new TargetDefibrillatedEvent(patient, patient, (defib, SComp<DefibrillatorComponent>(defib)), []);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(210f));

        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(120f));

        _heart.SetRate(uid, heart, 90f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f), "An automatic shock must stop at the resting rate.");

        _heart.SetRate(uid, heart, 70f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(80f));
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
}
