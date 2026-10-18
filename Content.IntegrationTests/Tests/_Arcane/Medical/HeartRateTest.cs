#nullable enable
using System.Linq;
using Content.Arcane.Common.Medical.Heart;
using Content.Arcane.Common.Medical.Defibrillator;
using Content.Arcane.Server.Medical.Defibrillator;
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
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Medical.Common.Targeting;
using Content.Shared.FixedPoint;
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

        SEntMan.RemoveComponent<HeartRateComponent>(uid);
        var unsimulated = new FindWorkingHeartEvent();
        SEntMan.EventBus.RaiseLocalEvent(patient, ref unsimulated);
        Assert.That(unsimulated.Found, Is.True, "A legacy heart must retain circulation without pulse simulation.");
        heart = SEntMan.AddComponent<HeartRateComponent>(uid);

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
        var ev = new DefibrillatorTreatmentEvent(defib, patient, true);
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
        var manual = SComp<ManualDefibrillatorComponent>(defib);
        var modes = SEntMan.System<Content.Arcane.Server.Medical.Defibrillator.ManualDefibrillatorSystem>();
        Assert.That(manual.Mode, Is.EqualTo(3));
        var ev = new DefibrillatorTreatmentEvent(defib, patient, true);
        _heart.SetRate(heartUid, heart, 0f, false);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(200f));
        Assert.That(modes.SetMode((defib, manual), 2), Is.True);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
        Assert.That(heart.CurrentRate, Is.EqualTo(150f));
        Assert.That(modes.SetMode((defib, manual), 5), Is.False);
        Assert.That(manual.Mode, Is.EqualTo(2));

        var verbs = SEntMan.System<Content.Shared.Verbs.SharedVerbSystem>()
            .GetLocalVerbs(defib, patient,
                new List<Type> { typeof(Content.Shared.Verbs.Verb), typeof(Content.Shared.Verbs.AlternativeVerb) }, force: true);
        var choices = verbs.Where(verb => verb.Category?.Text == Loc.GetString("arcane-defibrillator-mode-category")).ToArray();
        Assert.That(choices, Has.Length.EqualTo(4));
        Assert.That(choices.All(verb => verb.GetType() == typeof(Content.Shared.Verbs.Verb)), Is.True,
            "Mode choices must not participate in activation or alternative hotkeys, even without a battery.");
        Assert.That(verbs.Any(verb => verb is Content.Shared.Verbs.AlternativeVerb
            && verb.CompareTo(choices[0]) < 0), Is.True,
            "Battery ejection must remain above the mode choices for Alt+Z.");
        Assert.That(SComp<ItemToggleComponent>(defib).OnUse, Is.True, "Z must retain the normal power toggle.");
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
        var zap = new DefibrillatorTreatmentEvent(defib, patient, true);
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
    [RunOnSide(Side.Server)]
    public void ShockMedicineUsesTherapeuticConditionsAndDistinctReagents()
    {
        var patient = SSpawn("MobHuman");
        var blood = SComp<BloodstreamComponent>(patient);
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        Assert.That(solutions.ResolveSolution(patient, blood.BloodSolutionName, ref blood.BloodSolution, out var solution), Is.True);
        var medicine = SEntMan.System<OxygenMedicineSystem>();

        Add("Dexalin", 4);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.Zero);
        Add("Dexalin", 1);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.EqualTo(20));
        Add("DexalinPlus", 5);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.EqualTo(40));
        solutions.RemoveReagent(blood.BloodSolution!.Value, "DexalinPlus", 5);
        Add("Epinephrine", 5);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.EqualTo(40));
        Add("Epinephrine", 20);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.EqualTo(20), "An overdose must not qualify as critical healing.");
        solutions.RemoveReagent(blood.BloodSolution!.Value, "Epinephrine", 25);
        Add("Dexalin", 20);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.Zero, "Simultaneous damage must outweigh the healing branch in an overdose.");
        solutions.RemoveReagent(blood.BloodSolution!.Value, "Dexalin", 25);
        Add("Omnizine", 5);
        Assert.That(medicine.GetShockHealing(patient, blood.BloodSolution!.Value), Is.EqualTo(20), "Airloss group healing also treats suffocation.");
        Assert.That(SComp<MobStateComponent>(patient).CurrentState, Is.EqualTo(MobState.Alive));

        void Add(string reagent, int quantity)
            => Assert.That(solutions.TryAddReagent(blood.BloodSolution!.Value, reagent, quantity), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DeliveredShockTreatsBeforeRevivalAndPenalizesAnotherDevice()
    {
        var patient = SSpawn("MobHuman");
        var damage = SEntMan.System<DamageableSystem>();
        var mob = SEntMan.System<MobStateSystem>();
        var blood = SComp<BloodstreamComponent>(patient);
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        Assert.That(solutions.ResolveSolution(patient, blood.BloodSolutionName, ref blood.BloodSolution, out _), Is.True);
        Assert.That(solutions.TryAddReagent(blood.BloodSolution!.Value, "Epinephrine", 5), Is.True);
        damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = 60 } }, true,
            targetPart: TargetBodyPart.Vital);
        mob.ChangeMobState(patient, MobState.Dead);
        var heartUid = _body.GetOrgan(patient, "Heart")!.Value;
        var heart = SComp<HeartRateComponent>(heartUid);
        _heart.SetRate(heartUid, heart, 0f, false);

        var defib = SSpawn("DefibrillatorOneHandedUnpowered");
        Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(defib), Is.True);
        var system = SEntMan.System<SharedDefibrillatorSystem>();
        Assert.That(system.CanZap(defib, patient), Is.True);
        system.Zap(defib, patient, patient);
        Assert.That(damage.GetAllDamage(patient).DamageDict["Asphyxiation"], Is.EqualTo(FixedPoint2.New(20)));
        Assert.That(SComp<MobStateComponent>(patient).CurrentState, Is.Not.EqualTo(MobState.Dead));
        Assert.That(heart.CurrentRate, Is.GreaterThan(0f));
        Assert.That(blood.BloodSolution!.Value.Comp.Solution.GetTotalPrototypeQuantity("Epinephrine"),
            Is.EqualTo(FixedPoint2.New(5)));

        damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = 40 } }, true,
            targetPart: TargetBodyPart.Vital);
        mob.ChangeMobState(patient, MobState.Dead);
        var heat = damage.GetAllDamage(patient).DamageDict.GetValueOrDefault("Heat");
        var anotherDefib = SSpawn("DefibrillatorCompact");
        var anotherShock = new DefibrillatorTreatmentEvent(anotherDefib, SSpawn("MobHuman"), true);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref anotherShock);
        Assert.That(damage.GetAllDamage(patient).DamageDict["Asphyxiation"], Is.EqualTo(FixedPoint2.New(60)));
        Assert.That(damage.GetAllDamage(patient).DamageDict["Heat"] - heat, Is.EqualTo(FixedPoint2.New(30)));

        var stoppedPatient = SSpawn("MobHuman");
        mob.ChangeMobState(stoppedPatient, MobState.Dead);
        var stoppedHeartUid = _body.GetOrgan(stoppedPatient, "Heart")!.Value;
        _heart.SetRate(stoppedHeartUid, SComp<HeartRateComponent>(stoppedHeartUid), 0f, false);
        var loweringDefib = SSpawn("DefibrillatorOneHandedUnpowered");
        var manual = SEntMan.EnsureComponent<ManualDefibrillatorComponent>(loweringDefib);
        Assert.That(SEntMan.System<ManualDefibrillatorSystem>().SetMode((loweringDefib, manual), 2), Is.True);
        Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(loweringDefib), Is.True);
        system.Zap(loweringDefib, stoppedPatient, patient);
        Assert.That(SComp<MobStateComponent>(stoppedPatient).CurrentState, Is.EqualTo(MobState.Dead),
            "A mode which cannot restart the required heart must not report a revival.");
    }
}
