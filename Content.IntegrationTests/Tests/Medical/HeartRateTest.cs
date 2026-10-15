// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Shared.Medical.Heart;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Medical;

[TestOf(typeof(HeartRateSystem))]
public sealed class HeartRateTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Epinephrine = "Epinephrine";

    [SidedDependency(Side.Server)] private readonly HeartRateSystem _heart = null!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly SharedBuckleSystem _buckle = null!;

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

        var defib = SSpawn("Defibrillator");
        var ev = new TargetDefibrillatedEvent(patient, patient, (defib, SComp<DefibrillatorComponent>(defib)), []);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(210f));

        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(120f));
    }
}
// Arcane-End
