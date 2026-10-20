using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Server.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Bed.Components;
using Content.Shared.Buckle;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(OxygenKnockoutSystem))]
public sealed class OxygenKnockoutTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly IGameTiming _timing = null!;

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: MobHuman
  id: TestArcaneOxygenHuman
  components:
  - type: Respirator
    updateInterval: 1000
""";

    [Test]
    [RunOnSide(Side.Server)]
    public void OxygenSleepFollowsDamageThresholdsAndImmunityWithoutClearingSedation()
    {
        var patient = SSpawn("MobHuman");
        var thresholds = SEntMan.System<MobThresholdSystem>();
        Damage(patient, 49);
        Assert.That(Asleep(patient), Is.False);
        Damage(patient, 1);
        Assert.That(Asleep(patient), Is.True);
        Assert.That(SEntMan.System<MobStateSystem>().IsAlive(patient), Is.True);
        Assert.That(SEntMan.System<RespiratorSystem>().CanBreathe(patient, SComp<RespiratorComponent>(patient)), Is.True);

        thresholds.SetMobStateThreshold(patient, 120, MobState.SoftCrit);
        Assert.That(Asleep(patient), Is.False, "The same damage is below the new body's threshold.");
        Damage(patient, 10);
        Assert.That(Asleep(patient), Is.True);
        SEntMan.AddComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.RemoveComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        SEntMan.AddComponent<SpecialBreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.RemoveComponent<SpecialBreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        SEntMan.RemoveComponent<RespiratorComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.AddComponent<RespiratorComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        _effects.TrySetStatusEffectDuration(patient, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(10));
        Damage(patient, -1);
        Assert.That(Asleep(patient), Is.False);
        Assert.That(SEntMan.HasComponent<SleepingComponent>(patient), Is.True);
    }

    [Test]
    public async Task DeadBodiesAccumulateOxygenDamageOnlyAfterEffectiveTreatmentStops()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        float afterTreatment = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            Damage(patient, 20);
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = 5 } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true);
            SEntMan.System<MobStateSystem>().ChangeMobState(patient, MobState.Dead);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.EqualTo(20), "Ordinary dead bodies must not passively heal suffocation.");
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = -1 } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true);
        });
        await RunSeconds(2.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.InRange(21f, 22f), "Healing another damage type must not reset oxygen treatment grace.");
            Damage(patient, -5);
            afterTreatment = Oxygen(patient);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.EqualTo(afterTreatment)));
        await RunSeconds(2.2f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.GreaterThan(afterTreatment)));
    }

    [Test]
    public async Task PostmortemLimitsFollowTheBodyAndTreatmentRestartsCappedPatients()
    {
        await Pair.CreateTestMap();
        EntityUid capped = default, scaled = default, excessive = default, upperStage = default;
        await Server.WaitPost(() =>
        {
            capped = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            scaled = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            excessive = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            upperStage = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            _damage.ChangeDamage(capped, new() { DamageDict = { ["Asphyxiation"] = 99.9f } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true);
            Damage(scaled, 50);
            Damage(excessive, 130);
            Damage(upperStage, 75);
            SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(scaled, 120, MobState.SoftCrit);
            foreach (var body in new[] { capped, scaled, excessive, upperStage })
                SEntMan.System<MobStateSystem>().ChangeMobState(body, MobState.Dead);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(excessive), Is.False);
        });
        await RunSeconds(12.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.EqualTo(100));
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(capped), Is.False);
            Assert.That(Oxygen(excessive), Is.EqualTo(130), "Do not truncate existing damage above the cap.");
            Assert.That(Oxygen(upperStage), Is.InRange(75.6f, 75.91f), "The upper stage adds 0.3 damage per second.");
            Assert.That(Oxygen(scaled), Is.InRange(52f, 53f), "50 damage is still in the lower stage for this body.");
            Damage(capped, -1);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(capped), Is.True);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.EqualTo(99));
        });
        await RunSeconds(2.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.EqualTo(100));
        });
    }

    [Test]
    public async Task BriefStasisAndTreatmentDuringStasisPreserveTheExactGracePeriod()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid bed = default;
        TimeSpan deadline = default, pauseStart = default;
        await Server.WaitAssertion(() =>
        {
            patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            bed = SSpawnAtPosition("StasisBed", TestMap!.GridCoords);
            Damage(patient, 20);
            SEntMan.System<MobStateSystem>().ChangeMobState(patient, MobState.Dead);
        });
        await RunSeconds(0.1f);
        await Server.WaitAssertion(() =>
        {
            deadline = SComp<PostmortemHypoxiaComponent>(patient).NextDamage;
            pauseStart = _timing.CurTime;
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryBuckle(patient, null, bed), Is.True);
            Assert.That(SEntMan.HasComponent<StasisBedBuckledComponent>(patient), Is.True);
        });
        await RunSeconds(0.3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryUnbuckle(patient, null), Is.True);
            Assert.That(SEntMan.HasComponent<StasisBedBuckledComponent>(patient), Is.False);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(deadline - pauseStart),
                "Stasis preserves the remaining grace period without restarting ten seconds.");
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryBuckle(patient, null, bed), Is.True);
        });
        await RunSeconds(0.1f);
        await Server.WaitPost(() => Damage(patient, -1));
        await RunSeconds(0.3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryUnbuckle(patient, null), Is.True);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(TimeSpan.FromSeconds(10)),
                "Only effective oxygen treatment starts a new ten-second grace period, frozen until stasis ends.");

            var mobs = SEntMan.System<MobStateSystem>();
            mobs.ChangeMobState(patient, MobState.Alive);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(patient), Is.False);
            mobs.ChangeMobState(patient, MobState.Dead);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(TimeSpan.FromSeconds(10)),
                "A new death starts a fresh grace period.");
        });
    }

    [Test]
    public async Task AnalyzerWarningUsesThePatientsReplicatedKnockoutThreshold()
    {
        await Pair.CreateTestMap();
        NetEntity patientNet = default;
        EntityUid? previousAttachment = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                previousAttachment = ServerSession!.AttachedEntity;
                var patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
                SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(patient, 120, MobState.SoftCrit);
                Damage(patient, 59);
                patientNet = SEntMan.GetNetEntity(patient);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await AssertWarning(false);
            await Server.WaitPost(() => Damage(SEntMan.GetEntity(patientNet), 1));
            await AssertWarning(true);
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, previousAttachment));
        }

        async Task AssertWarning(bool expected)
        {
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var diagnostics = new HealthAnalyzerPopulateEvent();
                CEntMan.EventBus.RaiseLocalEvent(CEntMan.GetEntity(patientNet), ref diagnostics);
                Assert.That(diagnostics.Conditions.Contains(Loc.GetString("condition-body-asphyxiation-severe")), Is.EqualTo(expected));
            });
        }
    }

    private bool Asleep(EntityUid patient) => _effects.TryGetStatusEffect(patient, OxygenKnockoutSystem.OxygenSleep, out _);

    private float Oxygen(EntityUid patient) => _damage.GetAllDamage(patient).DamageDict.GetValueOrDefault("Asphyxiation").Float();

    private void Damage(EntityUid patient, int amount)
        => _damage.ChangeDamage(patient, new() { DamageDict = { ["Asphyxiation"] = amount } },
            targetPart: TargetBodyPart.Vital, ignoreResistances: true);
}
